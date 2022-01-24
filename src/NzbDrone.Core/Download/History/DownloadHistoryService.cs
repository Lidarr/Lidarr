using System;
using System.IO;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Download.History
{
    public interface IDownloadHistoryService
    {
        bool DownloadAlreadyImported(string downloadId);
        DownloadHistory GetLatestDownloadHistoryItem(string downloadId);
        DownloadHistory GetLatestGrab(string downloadId);
    }

    public class DownloadHistoryService : IDownloadHistoryService,
                                          IHandle<AlbumGrabbedEvent>,
                                          IHandle<TrackImportedEvent>,
                                          IHandle<AlbumImportIncompleteEvent>,
                                          IHandle<DownloadCompletedEvent>,
                                          IHandle<DownloadFailedEvent>,
                                          IHandle<DownloadIgnoredEvent>,
                                          IHandle<ArtistsDeletedEvent>
    {
        private readonly IDownloadHistoryRepository _repository;
        private readonly IHistoryService _historyService;

        public DownloadHistoryService(IDownloadHistoryRepository repository, IHistoryService historyService)
        {
            _repository = repository;
            _historyService = historyService;
        }

        public bool DownloadAlreadyImported(string downloadId)
        {
            var events = _repository.FindByDownloadId(downloadId);

            // Events are ordered by date descending, if a grabbed event comes before an imported event then it was never imported
            // or grabbed again after importing and should be reprocessed.
            foreach (var e in events)
            {
                if (e.EventType == DownloadHistoryEventType.DownloadGrabbed)
                {
                    return false;
                }

                if (e.EventType == DownloadHistoryEventType.DownloadImported)
                {
                    return true;
                }
            }

            return false;
        }

        public DownloadHistory GetLatestDownloadHistoryItem(string downloadId)
        {
            var events = _repository.FindByDownloadId(downloadId);

            // Events are ordered by date descending. We'll return the most recent expected event.
            foreach (var e in events)
            {
                if (e.EventType == DownloadHistoryEventType.DownloadIgnored)
                {
                    return e;
                }

                if (e.EventType == DownloadHistoryEventType.DownloadGrabbed)
                {
                    return e;
                }

                if (e.EventType == DownloadHistoryEventType.DownloadImported)
                {
                    return e;
                }

                if (e.EventType == DownloadHistoryEventType.DownloadFailed)
                {
                    return e;
                }

                if (e.EventType == DownloadHistoryEventType.DownloadImportIncomplete)
                {
                    return e;
                }
            }

            return null;
        }

        public DownloadHistory GetLatestGrab(string downloadId)
        {
            return _repository.FindByDownloadId(downloadId)
                              .FirstOrDefault(d => d.EventType == DownloadHistoryEventType.DownloadGrabbed);
        }

        public void Handle(AlbumGrabbedEvent message)
        {
            // Don't store grabbed events for clients that don't download IDs
            if (message.DownloadId.IsNullOrWhiteSpace())
            {
                return;
            }

            var history = new DownloadHistory
            {
                EventType = DownloadHistoryEventType.DownloadGrabbed,
                ArtistId = message.Album.Artist.Id,
                DownloadId = message.DownloadId,
                SourceTitle = message.Album.Release.Title,
                Date = DateTime.UtcNow,
                Protocol = message.Album.Release.DownloadProtocol,
                IndexerId = message.Album.Release.IndexerId,
                DownloadClientId = message.DownloadClientId,
                Release = message.Album.Release
            };

            history.Data.Add("Indexer", message.Album.Release.Indexer);
            history.Data.Add("DownloadClient", message.DownloadClient);
            history.Data.Add("DownloadClientName", message.DownloadClientName);
            history.Data.Add("CustomFormatScore", message.Album.CustomFormatScore.ToString());

            _repository.Insert(history);
        }

        public void Handle(TrackImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadId = message.DownloadId;

            // Try to find the downloadId if the user used manual import (from wanted: missing) or the
            // API to import and downloadId wasn't provided.
            if (downloadId.IsNullOrWhiteSpace())
            {
                downloadId = _historyService.FindDownloadId(message);
            }

            if (downloadId.IsNullOrWhiteSpace())
            {
                return;
            }

            var history = new DownloadHistory
            {
                EventType = DownloadHistoryEventType.FileImported,
                ArtistId = message.TrackInfo.Artist.Id,
                DownloadId = downloadId,
                SourceTitle = message.TrackInfo.Path,
                Date = DateTime.UtcNow,
                Protocol = message.DownloadClientInfo.Protocol,
                DownloadClientId = message.DownloadClientInfo.Id
            };

            history.Data.Add("DownloadClient", message.DownloadClientInfo.Type);
            history.Data.Add("DownloadClientName", message.DownloadClientInfo.Name);
            history.Data.Add("SourcePath", message.TrackInfo.Path);
            history.Data.Add("DestinationPath", message.ImportedTrack.Path);

            _repository.Insert(history);
        }

        public void Handle(AlbumImportIncompleteEvent message)
        {
            var history = new DownloadHistory
            {
                EventType = DownloadHistoryEventType.DownloadImportIncomplete,
                ArtistId = message.TrackedDownload.RemoteAlbum?.Artist.Id ?? 0,
                DownloadId = message.TrackedDownload.DownloadItem.DownloadId,
                SourceTitle = message.TrackedDownload.DownloadItem.OutputPath.ToString(),
                Date = DateTime.UtcNow,
                Protocol = message.TrackedDownload.Protocol,
                DownloadClientId = message.TrackedDownload.DownloadClient
            };

            history.Data.Add("DownloadClient", message.TrackedDownload.DownloadItem.DownloadClientInfo.Type);
            history.Data.Add("DownloadClientName", message.TrackedDownload.DownloadItem.DownloadClientInfo.Name);
            history.Data.Add("StatusMessages", message.TrackedDownload.StatusMessages.ToJson());

            _repository.Insert(history);
        }

        public void Handle(DownloadCompletedEvent message)
        {
            var history = new DownloadHistory
            {
                EventType = DownloadHistoryEventType.DownloadImported,
                ArtistId = message.TrackedDownload.RemoteAlbum.Artist.Id,
                DownloadId = message.TrackedDownload.DownloadItem.DownloadId,
                SourceTitle = message.TrackedDownload.DownloadItem.OutputPath.ToString(),
                Date = DateTime.UtcNow,
                Protocol = message.TrackedDownload.Protocol,
                DownloadClientId = message.TrackedDownload.DownloadClient
            };

            history.Data.Add("DownloadClient", message.TrackedDownload.DownloadItem.DownloadClientInfo.Type);
            history.Data.Add("DownloadClientName", message.TrackedDownload.DownloadItem.DownloadClientInfo.Name);

            _repository.Insert(history);
        }

        public void Handle(DownloadFailedEvent message)
        {
            // Don't track failed download for an unknown download
            if (message.TrackedDownload == null)
            {
                return;
            }

            var history = new DownloadHistory
            {
                EventType = DownloadHistoryEventType.DownloadFailed,
                ArtistId = message.ArtistId,
                DownloadId = message.DownloadId,
                SourceTitle = message.SourceTitle,
                Date = DateTime.UtcNow,
                Protocol = message.TrackedDownload.Protocol,
                DownloadClientId = message.TrackedDownload.DownloadClient
            };

            history.Data.Add("DownloadClient", message.TrackedDownload.DownloadItem.DownloadClientInfo.Type);
            history.Data.Add("DownloadClientName", message.TrackedDownload.DownloadItem.DownloadClientInfo.Name);

            _repository.Insert(history);
        }

        public void Handle(DownloadIgnoredEvent message)
        {
            var history = new DownloadHistory
            {
                EventType = DownloadHistoryEventType.DownloadIgnored,
                ArtistId = message.ArtistId,
                DownloadId = message.DownloadId,
                SourceTitle = message.SourceTitle,
                Date = DateTime.UtcNow,
                Protocol = message.DownloadClientInfo.Protocol,
                DownloadClientId = message.DownloadClientInfo.Id
            };

            history.Data.Add("DownloadClient", message.DownloadClientInfo.Type);
            history.Data.Add("DownloadClientName", message.DownloadClientInfo.Name);

            _repository.Insert(history);
        }

        public void Handle(ArtistsDeletedEvent message)
        {
            _repository.DeleteByArtistIds(message.Artists.Select(a => a.Id).ToList());
        }
    }
}
