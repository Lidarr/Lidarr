using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Extras.Metadata
{
    public class MetadataService : ExtraFileManager<MetadataFile>
    {
        private readonly IMetadataFactory _metadataFactory;
        private readonly ICleanMetadataService _cleanMetadataService;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IHttpClient _httpClient;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IMetadataFileService _metadataFileService;
        private readonly IAlbumService _albumService;
        private readonly Logger _logger;

        public MetadataService(IConfigService configService,
                               IDiskProvider diskProvider,
                               IDiskTransferService diskTransferService,
                               IMetadataFactory metadataFactory,
                               ICleanMetadataService cleanMetadataService,
                               IHttpClient httpClient,
                               IMediaFileAttributeService mediaFileAttributeService,
                               IMetadataFileService metadataFileService,
                               IAlbumService albumService,
                               Logger logger)
            : base(configService, diskProvider, diskTransferService, logger)
        {
            _metadataFactory = metadataFactory;
            _cleanMetadataService = cleanMetadataService;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _httpClient = httpClient;
            _mediaFileAttributeService = mediaFileAttributeService;
            _metadataFileService = metadataFileService;
            _albumService = albumService;
            _logger = logger;
        }

        public override int Order => 0;

        public override IEnumerable<ExtraFile> CreateAfterArtistScan(Artist artist, List<Album> albums, List<TrackFile> trackFiles)
        {
            var metadataFiles = _metadataFileService.GetFilesByArtist(artist.Id);
            _cleanMetadataService.Clean(artist);

            if (!_diskProvider.FolderExists(artist.Path))
            {
                _logger.Info("Artist folder does not exist, skipping metadata creation");
                return Enumerable.Empty<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                files.AddIfNotNull(ProcessArtistMetadata(consumer, artist, consumerFiles));
                files.AddRange(ProcessArtistImages(consumer, artist, consumerFiles));
                files.AddRange(ProcessAlbumImages(consumer, artist, consumerFiles));

                foreach (var album in albums)
                {
                    album.Artist = artist;
                    files.AddIfNotNull(ProcessAlbumMetadata(consumer, album, consumerFiles));
                }

                foreach (var trackFile in trackFiles)
                {
                    files.AddIfNotNull(ProcessEpisodeMetadata(consumer, artist, trackFile, consumerFiles));
                    files.AddRange(ProcessEpisodeImages(consumer, artist, trackFile, consumerFiles));
                }
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterTrackImport(Artist artist, TrackFile trackFile)
        {
            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {

                files.AddIfNotNull(ProcessEpisodeMetadata(consumer, artist, trackFile, new List<MetadataFile>()));
                files.AddRange(ProcessEpisodeImages(consumer, artist, trackFile, new List<MetadataFile>()));
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterTrackImport(Artist artist, string artistFolder, string albumFolder)
        {
            var metadataFiles = _metadataFileService.GetFilesByArtist(artist.Id);

            if (artistFolder.IsNullOrWhiteSpace() && albumFolder.IsNullOrWhiteSpace())
            {
                return new List<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                if (artistFolder.IsNotNullOrWhiteSpace())
                {
                    files.AddIfNotNull(ProcessArtistMetadata(consumer, artist, consumerFiles));
                    files.AddRange(ProcessArtistImages(consumer, artist, consumerFiles));
                }

                if (albumFolder.IsNotNullOrWhiteSpace())
                {
                    files.AddRange(ProcessAlbumImages(consumer, artist, consumerFiles));
                }
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> MoveFilesAfterRename(Artist artist, List<TrackFile> trackFiles)
        {
            var metadataFiles = _metadataFileService.GetFilesByArtist(artist.Id);
            var movedFiles = new List<MetadataFile>();

            // TODO: Move EpisodeImage and EpisodeMetadata metadata files, instead of relying on consumers to do it
            // (Xbmc's EpisodeImage is more than just the extension)

            foreach (var consumer in _metadataFactory.GetAvailableProviders())
            {
                foreach (var trackFile in trackFiles)
                {
                    var metadataFilesForConsumer = GetMetadataFilesForConsumer(consumer, metadataFiles).Where(m => m.TrackFileId == trackFile.Id).ToList();

                    foreach (var metadataFile in metadataFilesForConsumer)
                    {
                        var newFileName = consumer.GetFilenameAfterMove(artist, trackFile, metadataFile);
                        var existingFileName = Path.Combine(artist.Path, metadataFile.RelativePath);

                        if (newFileName.PathNotEquals(existingFileName))
                        {
                            try
                            {
                                _diskProvider.MoveFile(existingFileName, newFileName);
                                metadataFile.RelativePath = artist.Path.GetRelativePath(newFileName);
                                movedFiles.Add(metadataFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Unable to move metadata file after rename: {0}", existingFileName);
                            }
                        }
                    }
                }
            }

            _metadataFileService.Upsert(movedFiles);

            return movedFiles;
        }

        public override ExtraFile Import(Artist artist, TrackFile trackFile, string path, string extension, bool readOnly)
        {
            return null;
        }

        private List<MetadataFile> GetMetadataFilesForConsumer(IMetadata consumer, List<MetadataFile> artistMetadata)
        {
            return artistMetadata.Where(c => c.Consumer == consumer.GetType().Name).ToList();
        }

        private MetadataFile ProcessArtistMetadata(IMetadata consumer, Artist artist, List<MetadataFile> existingMetadataFiles)
        {
            var artistMetadata = consumer.ArtistMetadata(artist);

            if (artistMetadata == null)
            {
                return null;
            }

            var hash = artistMetadata.Contents.SHA256Hash();

            var metadata = GetMetadataFile(artist, existingMetadataFiles, e => e.Type == MetadataType.ArtistMetadata) ??
                               new MetadataFile
                               {
                                   ArtistId = artist.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.ArtistMetadata
                               };

            if (hash == metadata.Hash)
            {
                if (artistMetadata.RelativePath != metadata.RelativePath)
                {
                    metadata.RelativePath = artistMetadata.RelativePath;

                    return metadata;
                }

                return null;
            }

            var fullPath = Path.Combine(artist.Path, artistMetadata.RelativePath);

            _logger.Debug("Writing Artist Metadata to: {0}", fullPath);
            SaveMetadataFile(fullPath, artistMetadata.Contents);

            metadata.Hash = hash;
            metadata.RelativePath = artistMetadata.RelativePath;
            metadata.Extension = Path.GetExtension(fullPath);

            return metadata;
        }

        private MetadataFile ProcessAlbumMetadata(IMetadata consumer, Album album, List<MetadataFile> existingMetadataFiles)
        {
            var albumMetadata = consumer.AlbumMetadata(album.Artist, album);

            if (albumMetadata == null)
            {
                return null;
            }

            var hash = albumMetadata.Contents.SHA256Hash();

            var metadata = GetMetadataFile(album.Artist, existingMetadataFiles, e => e.Type == MetadataType.AlbumMetadata && e.AlbumId == album.Id) ??
                               new MetadataFile
                               {
                                   ArtistId = album.ArtistId,
                                   AlbumId = album.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.AlbumMetadata
                               };

            if (hash == metadata.Hash)
            {
                if (albumMetadata.RelativePath != metadata.RelativePath)
                {
                    metadata.RelativePath = albumMetadata.RelativePath;

                    return metadata;
                }

                return null;
            }

            var fullPath = Path.Combine(album.Path, albumMetadata.RelativePath);

            _logger.Debug("Writing Album Metadata to: {0}", fullPath);
            SaveMetadataFile(fullPath, albumMetadata.Contents);

            metadata.Hash = hash;
            metadata.RelativePath = albumMetadata.RelativePath;
            metadata.Extension = Path.GetExtension(fullPath);

            return metadata;
        }

        private MetadataFile ProcessEpisodeMetadata(IMetadata consumer, Artist artist, TrackFile trackFile, List<MetadataFile> existingMetadataFiles)
        {
            var episodeMetadata = consumer.TrackMetadata(artist, trackFile);

            if (episodeMetadata == null)
            {
                return null;
            }

            var fullPath = Path.Combine(artist.Path, episodeMetadata.RelativePath);

            var existingMetadata = GetMetadataFile(artist, existingMetadataFiles, c => c.Type == MetadataType.TrackMetadata &&
                                                                                  c.TrackFileId == trackFile.Id);

            if (existingMetadata != null)
            {
                var existingFullPath = Path.Combine(artist.Path, existingMetadata.RelativePath);
                if (fullPath.PathNotEquals(existingFullPath))
                {
                    _diskTransferService.TransferFile(existingFullPath, fullPath, TransferMode.Move);
                    existingMetadata.RelativePath = episodeMetadata.RelativePath;
                }
            }

            var hash = episodeMetadata.Contents.SHA256Hash();

            var metadata = existingMetadata ??
                           new MetadataFile
                           {
                               ArtistId = artist.Id,
                               AlbumId = trackFile.AlbumId,
                               TrackFileId = trackFile.Id,
                               Consumer = consumer.GetType().Name,
                               Type = MetadataType.TrackMetadata,
                               RelativePath = episodeMetadata.RelativePath,
                               Extension = Path.GetExtension(fullPath)
                           };

            if (hash == metadata.Hash)
            {
                return null;
            }

            _logger.Debug("Writing Track Metadata to: {0}", fullPath);
            SaveMetadataFile(fullPath, episodeMetadata.Contents);

            metadata.Hash = hash;

            return metadata;
        }

        private List<MetadataFile> ProcessArtistImages(IMetadata consumer, Artist artist, List<MetadataFile> existingMetadataFiles)
        {
            var result = new List<MetadataFile>();

            foreach (var image in consumer.ArtistImages(artist))
            {
                var fullPath = Path.Combine(artist.Path, image.RelativePath);

                if (_diskProvider.FileExists(fullPath))
                {
                    _logger.Debug("Artist image already exists: {0}", fullPath);
                    continue;
                }

                var metadata = GetMetadataFile(artist, existingMetadataFiles, c => c.Type == MetadataType.ArtistImage &&
                                                                              c.RelativePath == image.RelativePath) ??
                               new MetadataFile
                               {
                                   ArtistId = artist.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.ArtistImage,
                                   RelativePath = image.RelativePath,
                                   Extension = Path.GetExtension(fullPath)
                               };

                DownloadImage(artist, image);

                result.Add(metadata);
            }

            return result;
        }

        private List<MetadataFile> ProcessAlbumImages(IMetadata consumer, Artist artist, List<MetadataFile> existingMetadataFiles)
        {
            var result = new List<MetadataFile>();

            var albums = _albumService.GetAlbumsByArtist(artist.Id);

            foreach (var album in albums)
            {
                foreach (var image in consumer.AlbumImages(artist, album))
                {
                    var fullPath = Path.Combine(artist.Path, image.RelativePath);

                    if (_diskProvider.FileExists(fullPath))
                    {
                        _logger.Debug("Album image already exists: {0}", fullPath);
                        continue;
                    }

                    var metadata = GetMetadataFile(artist, existingMetadataFiles, c => c.Type == MetadataType.AlbumImage &&
                                                                                  c.AlbumId == album.Id &&
                                                                                  c.RelativePath == image.RelativePath) ??
                                new MetadataFile
                                {
                                    ArtistId = artist.Id,
                                    AlbumId = album.Id,
                                    Consumer = consumer.GetType().Name,
                                    Type = MetadataType.AlbumImage,
                                    RelativePath = image.RelativePath,
                                    Extension = Path.GetExtension(fullPath)
                                };

                    DownloadImage(artist, image);

                    result.Add(metadata);
                }
            }

            return result;
        }

        private List<MetadataFile> ProcessEpisodeImages(IMetadata consumer, Artist artist, TrackFile trackFile, List<MetadataFile> existingMetadataFiles)
        {
            var result = new List<MetadataFile>();

            foreach (var image in consumer.TrackImages(artist, trackFile))
            {
                var fullPath = Path.Combine(artist.Path, image.RelativePath);

                if (_diskProvider.FileExists(fullPath))
                {
                    _logger.Debug("Track image already exists: {0}", fullPath);
                    continue;
                }

                var existingMetadata = GetMetadataFile(artist, existingMetadataFiles, c => c.Type == MetadataType.TrackImage &&
                                                                                      c.TrackFileId == trackFile.Id);

                if (existingMetadata != null)
                {
                    var existingFullPath = Path.Combine(artist.Path, existingMetadata.RelativePath);
                    if (fullPath.PathNotEquals(existingFullPath))
                    {
                        _diskTransferService.TransferFile(existingFullPath, fullPath, TransferMode.Move);
                        existingMetadata.RelativePath = image.RelativePath;

                        return new List<MetadataFile>{ existingMetadata };
                    }
                }

                var metadata = existingMetadata ??
                               new MetadataFile
                               {
                                   ArtistId = artist.Id,
                                   AlbumId = trackFile.AlbumId,
                                   TrackFileId = trackFile.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.TrackImage,
                                   RelativePath = image.RelativePath,
                                   Extension = Path.GetExtension(fullPath)
                               };

                DownloadImage(artist, image);

                result.Add(metadata);
            }

            return result;
        }

        private void DownloadImage(Artist artist, ImageFileResult image)
        {
            var fullPath = Path.Combine(artist.Path, image.RelativePath);

            try
            {
                if (image.Url.StartsWith("http"))
                {
                    _httpClient.DownloadFile(image.Url, fullPath);
                }
                else
                {
                    _diskProvider.CopyFile(image.Url, fullPath);
                }
                _mediaFileAttributeService.SetFilePermissions(fullPath);
            }
            catch (WebException ex)
            {
                _logger.Warn(ex, "Couldn't download image {0} for {1}. {2}", image.Url, artist, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Couldn't download image {0} for {1}. {2}", image.Url, artist, ex.Message);
            }
        }

        private void SaveMetadataFile(string path, string contents)
        {
            _diskProvider.WriteAllText(path, contents);
            _mediaFileAttributeService.SetFilePermissions(path);
        }

        private MetadataFile GetMetadataFile(Artist artist, List<MetadataFile> existingMetadataFiles, Func<MetadataFile, bool> predicate)
        {
            var matchingMetadataFiles = existingMetadataFiles.Where(predicate).ToList();

            if (matchingMetadataFiles.Empty())
            {
                return null;
            }

            //Remove duplicate metadata files from DB and disk
            foreach (var file in matchingMetadataFiles.Skip(1))
            {
                var path = Path.Combine(artist.Path, file.RelativePath);

                _logger.Debug("Removing duplicate Metadata file: {0}", path);

                _diskProvider.DeleteFile(path);
                _metadataFileService.Delete(file.Id);
            }

            
            return matchingMetadataFiles.First();
        }
    }
}
