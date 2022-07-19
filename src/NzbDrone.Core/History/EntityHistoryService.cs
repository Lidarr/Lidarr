using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryService
    {
        PagingSpec<EntityHistory> Paged(PagingSpec<EntityHistory> pagingSpec, int[] qualities);
        EntityHistory MostRecentForAlbum(int albumId);
        EntityHistory MostRecentForDownloadId(string downloadId);
        EntityHistory Get(int historyId);
        List<EntityHistory> GetByArtist(int artistId, EntityHistoryEventType? eventType);
        List<EntityHistory> GetByAlbum(int albumId, EntityHistoryEventType? eventType);
        List<EntityHistory> Find(string downloadId, EntityHistoryEventType eventType);
        List<EntityHistory> FindByDownloadId(string downloadId);
        string FindDownloadId(TrackImportedEvent trackedDownload);
        List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType);
        void UpdateMany(IList<EntityHistory> items);
    }

    public class EntityHistoryService : IHistoryService,
                                  IHandle<AlbumGrabbedEvent>,
                                  IHandle<AlbumImportIncompleteEvent>,
                                  IHandle<TrackImportedEvent>,
                                  IHandle<DownloadFailedEvent>,
                                  IHandle<DownloadCompletedEvent>,
                                  IHandle<TrackFileDeletedEvent>,
                                  IHandle<TrackFileRenamedEvent>,
                                  IHandle<TrackFileRetaggedEvent>,
                                  IHandle<ArtistsDeletedEvent>,
                                  IHandle<DownloadIgnoredEvent>
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly Logger _logger;

        public EntityHistoryService(IHistoryRepository historyRepository, Logger logger)
        {
            _historyRepository = historyRepository;
            _logger = logger;
        }

        public PagingSpec<EntityHistory> Paged(PagingSpec<EntityHistory> pagingSpec, int[] qualities)
        {
            return _historyRepository.GetPaged(pagingSpec, qualities);
        }

        public EntityHistory MostRecentForAlbum(int albumId)
        {
            return _historyRepository.MostRecentForAlbum(albumId);
        }

        public EntityHistory MostRecentForDownloadId(string downloadId)
        {
            return _historyRepository.MostRecentForDownloadId(downloadId);
        }

        public EntityHistory Get(int historyId)
        {
            return _historyRepository.Get(historyId);
        }

        public List<EntityHistory> GetByArtist(int artistId, EntityHistoryEventType? eventType)
        {
            return _historyRepository.GetByArtist(artistId, eventType);
        }

        public List<EntityHistory> GetByAlbum(int albumId, EntityHistoryEventType? eventType)
        {
            return _historyRepository.GetByAlbum(albumId, eventType);
        }

        public List<EntityHistory> Find(string downloadId, EntityHistoryEventType eventType)
        {
            return _historyRepository.FindByDownloadId(downloadId).Where(c => c.EventType == eventType).ToList();
        }

        public List<EntityHistory> FindByDownloadId(string downloadId)
        {
            return _historyRepository.FindByDownloadId(downloadId);
        }

        public string FindDownloadId(TrackImportedEvent trackedDownload)
        {
            _logger.Debug("Trying to find downloadId for {0} from history", trackedDownload.ImportedTrack.Path);

            var albumIds = trackedDownload.TrackInfo.Tracks.Select(c => c.AlbumId).ToList();

            var allHistory = _historyRepository.FindDownloadHistory(trackedDownload.TrackInfo.Artist.Id, trackedDownload.ImportedTrack.Quality);

            // Find download related items for these episodes
            var albumsHistory = allHistory.Where(h => albumIds.Contains(h.AlbumId)).ToList();

            var processedDownloadId = albumsHistory
                .Where(c => c.EventType != EntityHistoryEventType.Grabbed && c.DownloadId != null)
                .Select(c => c.DownloadId);

            var stillDownloading = albumsHistory.Where(c => c.EventType == EntityHistoryEventType.Grabbed && !processedDownloadId.Contains(c.DownloadId)).ToList();

            string downloadId = null;

            if (stillDownloading.Any())
            {
                foreach (var matchingHistory in trackedDownload.TrackInfo.Tracks.Select(e => stillDownloading.Where(c => c.AlbumId == e.AlbumId).ToList()))
                {
                    if (matchingHistory.Count != 1)
                    {
                        return null;
                    }

                    var newDownloadId = matchingHistory.Single().DownloadId;

                    if (downloadId == null || downloadId == newDownloadId)
                    {
                        downloadId = newDownloadId;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return downloadId;
        }

        public void Handle(AlbumGrabbedEvent message)
        {
            foreach (var album in message.Album.Albums)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.Grabbed,
                    Date = DateTime.UtcNow,
                    Quality = message.Album.ParsedAlbumInfo.Quality,
                    SourceTitle = message.Album.Release.Title,
                    ArtistId = album.ArtistId,
                    AlbumId = album.Id,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("Indexer", message.Album.Release.Indexer);
                history.Data.Add("NzbInfoUrl", message.Album.Release.InfoUrl);
                history.Data.Add("ReleaseGroup", message.Album.ParsedAlbumInfo.ReleaseGroup);
                history.Data.Add("Age", message.Album.Release.Age.ToString());
                history.Data.Add("AgeHours", message.Album.Release.AgeHours.ToString());
                history.Data.Add("AgeMinutes", message.Album.Release.AgeMinutes.ToString());
                history.Data.Add("PublishedDate", message.Album.Release.PublishDate.ToUniversalTime().ToString("s") + "Z");
                history.Data.Add("DownloadClient", message.DownloadClient);
                history.Data.Add("Size", message.Album.Release.Size.ToString());
                history.Data.Add("DownloadUrl", message.Album.Release.DownloadUrl);
                history.Data.Add("Guid", message.Album.Release.Guid);
                history.Data.Add("Protocol", message.Album.Release.DownloadProtocol.ToString());
                history.Data.Add("DownloadForced", (!message.Album.DownloadAllowed).ToString());
                history.Data.Add("CustomFormatScore", message.Album.CustomFormatScore.ToString());
                history.Data.Add("ReleaseSource", message.Album.ReleaseSource.ToString());
                history.Data.Add("IndexerFlags", message.Album.Release.IndexerFlags.ToString());

                if (!message.Album.ParsedAlbumInfo.ReleaseHash.IsNullOrWhiteSpace())
                {
                    history.Data.Add("ReleaseHash", message.Album.ParsedAlbumInfo.ReleaseHash);
                }

                if (message.Album.Release is TorrentInfo torrentRelease)
                {
                    history.Data.Add("TorrentInfoHash", torrentRelease.InfoHash);
                }

                _historyRepository.Insert(history);
            }
        }

        public void Handle(AlbumImportIncompleteEvent message)
        {
            if (message.TrackedDownload.RemoteAlbum == null)
            {
                return;
            }

            foreach (var album in message.TrackedDownload.RemoteAlbum.Albums)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.AlbumImportIncomplete,
                    Date = DateTime.UtcNow,
                    Quality = message.TrackedDownload.RemoteAlbum.ParsedAlbumInfo?.Quality ?? new QualityModel(),
                    SourceTitle = message.TrackedDownload.DownloadItem.Title,
                    ArtistId = album.ArtistId,
                    AlbumId = album.Id,
                    DownloadId = message.TrackedDownload.DownloadItem.DownloadId
                };

                history.Data.Add("StatusMessages", message.TrackedDownload.StatusMessages.ToJson());
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteAlbum?.ParsedAlbumInfo?.ReleaseGroup);
                history.Data.Add("IndexerFlags", message.TrackedDownload?.RemoteAlbum?.Release?.IndexerFlags.ToString());

                _historyRepository.Insert(history);
            }
        }

        public void Handle(TrackImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadId = message.DownloadId;

            if (downloadId.IsNullOrWhiteSpace())
            {
                downloadId = FindDownloadId(message);
            }

            foreach (var track in message.TrackInfo.Tracks)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.TrackFileImported,
                    Date = DateTime.UtcNow,
                    Quality = message.TrackInfo.Quality,
                    SourceTitle = message.ImportedTrack.SceneName ?? Path.GetFileNameWithoutExtension(message.TrackInfo.Path),
                    ArtistId = message.TrackInfo.Artist.Id,
                    AlbumId = message.TrackInfo.Album.Id,
                    TrackId = track.Id,
                    DownloadId = downloadId
                };

                history.Data.Add("FileId", message.ImportedTrack.Id.ToString());
                history.Data.Add("DroppedPath", message.TrackInfo.Path);
                history.Data.Add("ImportedPath", message.ImportedTrack.Path);
                history.Data.Add("DownloadClient", message.DownloadClientInfo?.Name);
                history.Data.Add("ReleaseGroup", message.TrackInfo.ReleaseGroup);
                history.Data.Add("Size", message.TrackInfo.Size.ToString());
                history.Data.Add("IndexerFlags", message.ImportedTrack.IndexerFlags.ToString());

                _historyRepository.Insert(history);
            }
        }

        public void Handle(DownloadFailedEvent message)
        {
            foreach (var albumId in message.AlbumIds)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.DownloadFailed,
                    Date = DateTime.UtcNow,
                    Quality = message.Quality,
                    SourceTitle = message.SourceTitle,
                    ArtistId = message.ArtistId,
                    AlbumId = albumId,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("DownloadClient", message.DownloadClient);
                history.Data.Add("Message", message.Message);
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteAlbum?.ParsedAlbumInfo?.ReleaseGroup ?? message.Data.GetValueOrDefault(EntityHistory.RELEASE_GROUP));
                history.Data.Add("Size", message.TrackedDownload?.DownloadItem.TotalSize.ToString() ?? message.Data.GetValueOrDefault(EntityHistory.SIZE));
                history.Data.Add("Indexer", message.TrackedDownload?.RemoteAlbum?.Release?.Indexer ?? message.Data.GetValueOrDefault(EntityHistory.INDEXER));

                _historyRepository.Insert(history);
            }
        }

        public void Handle(DownloadCompletedEvent message)
        {
            if (message.TrackedDownload.RemoteAlbum == null)
            {
                return;
            }

            foreach (var album in message.TrackedDownload.RemoteAlbum.Albums)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.DownloadImported,
                    Date = DateTime.UtcNow,
                    Quality = message.TrackedDownload.RemoteAlbum.ParsedAlbumInfo?.Quality ?? new QualityModel(),
                    SourceTitle = message.TrackedDownload.DownloadItem.Title,
                    ArtistId = album.ArtistId,
                    AlbumId = album.Id,
                    DownloadId = message.TrackedDownload.DownloadItem.DownloadId
                };

                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteAlbum?.ParsedAlbumInfo?.ReleaseGroup);

                _historyRepository.Insert(history);
            }
        }

        public void Handle(TrackFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.NoLinkedEpisodes)
            {
                _logger.Debug("Removing track file from DB as part of cleanup routine, not creating history event.");
                return;
            }
            else if (message.Reason == DeleteMediaFileReason.ManualOverride)
            {
                _logger.Debug("Removing track file from DB as part of manual override of existing file, not creating history event.");
                return;
            }

            foreach (var track in message.TrackFile.Tracks.Value)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.TrackFileDeleted,
                    Date = DateTime.UtcNow,
                    Quality = message.TrackFile.Quality,
                    SourceTitle = message.TrackFile.Path,
                    ArtistId = message.TrackFile.Artist.Value.Id,
                    AlbumId = message.TrackFile.AlbumId,
                    TrackId = track.Id,
                };

                history.Data.Add("Reason", message.Reason.ToString());
                history.Data.Add("ReleaseGroup", message.TrackFile.ReleaseGroup);
                history.Data.Add("Size", message.TrackFile.Size.ToString());
                history.Data.Add("IndexerFlags", message.TrackFile.IndexerFlags.ToString());

                _historyRepository.Insert(history);
            }
        }

        public void Handle(TrackFileRenamedEvent message)
        {
            var sourcePath = message.OriginalPath;
            var path = message.TrackFile.Path;

            foreach (var track in message.TrackFile.Tracks.Value)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.TrackFileRenamed,
                    Date = DateTime.UtcNow,
                    Quality = message.TrackFile.Quality,
                    SourceTitle = message.OriginalPath,
                    ArtistId = message.TrackFile.Artist.Value.Id,
                    AlbumId = message.TrackFile.AlbumId,
                    TrackId = track.Id,
                };

                history.Data.Add("SourcePath", sourcePath);
                history.Data.Add("Path", path);
                history.Data.Add("ReleaseGroup", message.TrackFile.ReleaseGroup);
                history.Data.Add("Size", message.TrackFile.Size.ToString());
                history.Data.Add("IndexerFlags", message.TrackFile.IndexerFlags.ToString());

                _historyRepository.Insert(history);
            }
        }

        public void Handle(TrackFileRetaggedEvent message)
        {
            var path = message.TrackFile.Path;

            foreach (var track in message.TrackFile.Tracks.Value)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.TrackFileRetagged,
                    Date = DateTime.UtcNow,
                    Quality = message.TrackFile.Quality,
                    SourceTitle = path,
                    ArtistId = message.TrackFile.Artist.Value.Id,
                    AlbumId = message.TrackFile.AlbumId,
                    TrackId = track.Id,
                };

                history.Data.Add("TagsScrubbed", message.Scrubbed.ToString());
                history.Data.Add("ReleaseGroup", message.TrackFile.ReleaseGroup);
                history.Data.Add("Diff", message.Diff.Select(x => new
                {
                    Field = x.Key,
                    OldValue = x.Value.Item1,
                    NewValue = x.Value.Item2
                }).ToJson());

                _historyRepository.Insert(history);
            }
        }

        public void Handle(ArtistsDeletedEvent message)
        {
            _historyRepository.DeleteForArtists(message.Artists.Select(x => x.Id).ToList());
        }

        public void Handle(DownloadIgnoredEvent message)
        {
            var historyToAdd = new List<EntityHistory>();
            foreach (var albumId in message.AlbumIds)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.DownloadIgnored,
                    Date = DateTime.UtcNow,
                    Quality = message.Quality,
                    SourceTitle = message.SourceTitle,
                    ArtistId = message.ArtistId,
                    AlbumId = albumId,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("DownloadClient", message.DownloadClientInfo?.Name);
                history.Data.Add("Message", message.Message);
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteAlbum?.ParsedAlbumInfo?.ReleaseGroup);
                history.Data.Add("Size", message.TrackedDownload?.DownloadItem.TotalSize.ToString());
                history.Data.Add("Indexer", message.TrackedDownload?.RemoteAlbum?.Release?.Indexer);

                historyToAdd.Add(history);
            }

            _historyRepository.InsertMany(historyToAdd);
        }

        public List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType)
        {
            return _historyRepository.Since(date, eventType);
        }

        public void UpdateMany(IList<EntityHistory> items)
        {
            _historyRepository.UpdateMany(items);
        }
    }
}
