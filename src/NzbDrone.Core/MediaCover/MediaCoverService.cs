using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.MediaCover
{
    public interface IMapCoversToLocal
    {
        void ConvertToLocalUrls(int entityId, MediaCoverEntity coverEntity, ICollection<MediaCover> covers);
        string GetCoverPath(int entityId, MediaCoverEntity coverEntity, MediaCoverTypes coverType, string extension, int? height = null);
        bool EnsureAlbumCovers(Album album);
    }

    public class MediaCoverService :
        IHandleAsync<ArtistRefreshCompleteEvent>,
        IHandleAsync<ArtistsDeletedEvent>,
        IHandleAsync<AlbumAddedEvent>,
        IHandleAsync<AlbumDeletedEvent>,
        IMapCoversToLocal
    {
        private readonly IMediaCoverProxy _mediaCoverProxy;
        private readonly IImageResizer _resizer;
        private readonly IAlbumService _albumService;
        private readonly IHttpClient _httpClient;
        private readonly IDiskProvider _diskProvider;
        private readonly ICoverExistsSpecification _coverExistsSpecification;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        private readonly string _coverRootFolder;

        // ImageSharp is slow on ARM (no hardware acceleration on mono yet)
        // So limit the number of concurrent resizing tasks
        private static SemaphoreSlim _semaphore = new SemaphoreSlim((int)Math.Ceiling(Environment.ProcessorCount / 2.0));

        public MediaCoverService(IMediaCoverProxy mediaCoverProxy,
                                 IImageResizer resizer,
                                 IAlbumService albumService,
                                 IHttpClient httpClient,
                                 IDiskProvider diskProvider,
                                 IAppFolderInfo appFolderInfo,
                                 ICoverExistsSpecification coverExistsSpecification,
                                 IConfigFileProvider configFileProvider,
                                 IEventAggregator eventAggregator,
                                 Logger logger)
        {
            _mediaCoverProxy = mediaCoverProxy;
            _resizer = resizer;
            _albumService = albumService;
            _httpClient = httpClient;
            _diskProvider = diskProvider;
            _coverExistsSpecification = coverExistsSpecification;
            _configFileProvider = configFileProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;

            _coverRootFolder = appFolderInfo.GetMediaCoverPath();
        }

        public string GetCoverPath(int entityId, MediaCoverEntity coverEntity, MediaCoverTypes coverType, string extension, int? height = null)
        {
            var heightSuffix = height.HasValue ? "-" + height.ToString() : "";

            if (coverEntity == MediaCoverEntity.Album)
            {
                return Path.Combine(GetAlbumCoverPath(entityId), coverType.ToString().ToLower() + heightSuffix + GetExtension(coverType, extension));
            }

            return Path.Combine(GetArtistCoverPath(entityId), coverType.ToString().ToLower() + heightSuffix + GetExtension(coverType, extension));
        }

        public void ConvertToLocalUrls(int entityId, MediaCoverEntity coverEntity, ICollection<MediaCover> covers)
        {
            if (entityId == 0)
            {
                // Artist isn't in Lidarr yet, map via a proxy to circument referrer issues
                foreach (var mediaCover in covers)
                {
                    mediaCover.RemoteUrl = mediaCover.Url;
                    mediaCover.Url = _mediaCoverProxy.RegisterUrl(mediaCover.RemoteUrl);
                }

                return;
            }

            if (!covers.Any())
            {
                PopulateCoverWithCache(entityId, coverEntity, covers);
            }

            foreach (var mediaCover in covers)
            {
                if (mediaCover.CoverType == MediaCoverTypes.Unknown)
                {
                    continue;
                }

                var filePath = GetCoverPath(entityId, coverEntity, mediaCover.CoverType, mediaCover.Extension, null);

                mediaCover.RemoteUrl = mediaCover.Url;

                if (coverEntity == MediaCoverEntity.Album)
                {
                    mediaCover.Url = _configFileProvider.UrlBase + @"/MediaCover/Albums/" + entityId + "/" + mediaCover.CoverType.ToString().ToLower() + GetExtension(mediaCover.CoverType, mediaCover.Extension);
                }
                else
                {
                    mediaCover.Url = _configFileProvider.UrlBase + @"/MediaCover/" + entityId + "/" + mediaCover.CoverType.ToString().ToLower() + GetExtension(mediaCover.CoverType, mediaCover.Extension);
                }

                if (_diskProvider.FileExists(filePath))
                {
                    var lastWrite = _diskProvider.FileGetLastWrite(filePath);
                    mediaCover.Url += "?lastWrite=" + lastWrite.Ticks;
                }
            }
        }

        private string GetArtistCoverPath(int artistId)
        {
            return Path.Combine(_coverRootFolder, artistId.ToString());
        }

        private string GetAlbumCoverPath(int albumId)
        {
            return Path.Combine(_coverRootFolder, "Albums", albumId.ToString());
        }

        private bool EnsureArtistCovers(Artist artist)
        {
            var updated = false;
            var toResize = new List<Tuple<MediaCover, bool>>();

            foreach (var cover in artist.Metadata.Value.Images)
            {
                if (cover.CoverType == MediaCoverTypes.Unknown)
                {
                    continue;
                }

                var fileName = GetCoverPath(artist.Id, MediaCoverEntity.Artist, cover.CoverType, cover.Extension);
                var alreadyExists = false;

                try
                {
                    var serverFileHeaders = _httpClient.Head(new HttpRequest(cover.Url) { AllowAutoRedirect = true }).Headers;

                    alreadyExists = _coverExistsSpecification.AlreadyExists(serverFileHeaders.LastModified, serverFileHeaders.ContentLength, fileName);

                    if (!alreadyExists)
                    {
                        DownloadCover(artist, cover, serverFileHeaders.LastModified ?? DateTime.Now);
                        updated = true;
                    }
                }
                catch (HttpException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", artist, e.Message);
                }
                catch (WebException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", artist, e.Message);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't download media cover for {0}", artist);
                }

                toResize.Add(Tuple.Create(cover, alreadyExists));
            }

            try
            {
                _semaphore.Wait();

                foreach (var tuple in toResize)
                {
                    EnsureResizedCovers(artist, tuple.Item1, !tuple.Item2);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return updated;
        }

        private void PopulateCoverWithCache(int entityId, MediaCoverEntity coverEntity, ICollection<MediaCover> covers)
        {
            var folderPath = coverEntity == MediaCoverEntity.Album ? GetAlbumCoverPath(entityId) : GetArtistCoverPath(entityId);

            if (_diskProvider.FolderExists(folderPath))
            {
                foreach (var fileInfo in _diskProvider.GetFileInfos(folderPath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    var extension = Path.GetExtension(fileInfo.Name);
                    if (fileName.Contains('-'))
                    {
                        continue;
                    }

                    if (Enum.TryParse(fileName, true, out MediaCoverTypes coverType) && !covers.Any(c => c.CoverType == coverType))
                    {
                        var filePath = fileInfo.FullName;
                        var diskCover = new MediaCover(coverType, filePath)
                        {
                            RemoteUrl = filePath
                        };

                        covers.Add(diskCover);
                    }
                }
            }
        }

        public bool EnsureAlbumCovers(Album album)
        {
            var updated = false;

            foreach (var cover in album.Images.Where(e => e.CoverType == MediaCoverTypes.Cover))
            {
                if (cover.CoverType == MediaCoverTypes.Unknown)
                {
                    continue;
                }

                var fileName = GetCoverPath(album.Id, MediaCoverEntity.Album, cover.CoverType, cover.Extension, null);
                var alreadyExists = false;

                try
                {
                    var serverFileHeaders = _httpClient.Head(new HttpRequest(cover.Url) { AllowAutoRedirect = true }).Headers;

                    alreadyExists = _coverExistsSpecification.AlreadyExists(serverFileHeaders.LastModified, serverFileHeaders.ContentLength, fileName);

                    if (!alreadyExists)
                    {
                        DownloadAlbumCover(album, cover, serverFileHeaders.LastModified ?? DateTime.Now);
                        updated = true;
                    }
                }
                catch (HttpException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", album, e.Message);
                }
                catch (WebException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", album, e.Message);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't download media cover for {0}", album);
                }
            }

            return updated;
        }

        private void DownloadCover(Artist artist, MediaCover cover, DateTime lastModified)
        {
            var fileName = GetCoverPath(artist.Id, MediaCoverEntity.Artist, cover.CoverType, cover.Extension);

            _logger.Info("Downloading {0} for {1} {2}", cover.CoverType, artist, cover.Url);
            _httpClient.DownloadFile(cover.Url, fileName);

            try
            {
                _diskProvider.FileSetLastWriteTime(fileName, lastModified);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Unable to set modified date for {0} image for artist {1}", cover.CoverType, artist);
            }
        }

        private void DownloadAlbumCover(Album album, MediaCover cover, DateTime lastModified)
        {
            var fileName = GetCoverPath(album.Id, MediaCoverEntity.Album, cover.CoverType, cover.Extension, null);

            _logger.Info("Downloading {0} for {1} {2}", cover.CoverType, album, cover.Url);
            _httpClient.DownloadFile(cover.Url, fileName);

            try
            {
                _diskProvider.FileSetLastWriteTime(fileName, lastModified);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Unable to set modified date for {0} image for album {1}", cover.CoverType, album);
            }
        }

        private void EnsureResizedCovers(Artist artist, MediaCover cover, bool forceResize, Album album = null)
        {
            var heights = GetDefaultHeights(cover.CoverType);

            foreach (var height in heights)
            {
                var mainFileName = GetCoverPath(artist.Id, MediaCoverEntity.Artist, cover.CoverType, cover.Extension);
                var resizeFileName = GetCoverPath(artist.Id, MediaCoverEntity.Artist, cover.CoverType, cover.Extension, height);

                if (forceResize || !_diskProvider.FileExists(resizeFileName) || _diskProvider.GetFileSize(resizeFileName) == 0)
                {
                    _logger.Debug("Resizing {0}-{1} for {2}", cover.CoverType, height, artist);

                    try
                    {
                        _resizer.Resize(mainFileName, resizeFileName, height);
                    }
                    catch
                    {
                        _logger.Debug("Couldn't resize media cover {0}-{1} for artist {2}, using full size image instead.", cover.CoverType, height, artist);
                    }
                }
            }
        }

        private int[] GetDefaultHeights(MediaCoverTypes coverType)
        {
            switch (coverType)
            {
                default:
                    return Array.Empty<int>();

                case MediaCoverTypes.Poster:
                case MediaCoverTypes.Disc:
                case MediaCoverTypes.Cover:
                case MediaCoverTypes.Logo:
                case MediaCoverTypes.Headshot:
                    return new[] { 500, 250 };

                case MediaCoverTypes.Banner:
                    return new[] { 70, 35 };

                case MediaCoverTypes.Fanart:
                case MediaCoverTypes.Screenshot:
                    return new[] { 360, 180 };
            }
        }

        private string GetExtension(MediaCoverTypes coverType, string defaultExtension)
        {
            return coverType switch
            {
                MediaCoverTypes.Clearlogo => ".png",
                _ => defaultExtension
            };
        }

        public void HandleAsync(ArtistRefreshCompleteEvent message)
        {
            var updated = EnsureArtistCovers(message.Artist);

            var albums = _albumService.GetAlbumsByArtist(message.Artist.Id);
            foreach (var album in albums)
            {
                updated |= EnsureAlbumCovers(album);
            }

            _eventAggregator.PublishEvent(new MediaCoversUpdatedEvent(message.Artist, updated));
        }

        public void HandleAsync(ArtistsDeletedEvent message)
        {
            foreach (var artist in message.Artists)
            {
                var path = GetArtistCoverPath(artist.Id);
                if (_diskProvider.FolderExists(path))
                {
                    _diskProvider.DeleteFolder(path, true);
                }
            }
        }

        public void HandleAsync(AlbumAddedEvent message)
        {
            if (message.DoRefresh)
            {
                var updated = EnsureAlbumCovers(message.Album);

                _eventAggregator.PublishEvent(new MediaCoversUpdatedEvent(message.Album, updated));
            }
        }

        public void HandleAsync(AlbumDeletedEvent message)
        {
            var path = GetAlbumCoverPath(message.Album.Id);
            if (_diskProvider.FolderExists(path))
            {
                _diskProvider.DeleteFolder(path, true);
            }
        }
    }
}
