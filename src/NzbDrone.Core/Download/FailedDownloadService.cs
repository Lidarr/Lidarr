using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public interface IFailedDownloadService
    {
        void MarkAsFailed(int historyId, bool skipRedownload = false);
        void MarkAsFailed(TrackedDownload trackedDownload, bool skipRedownload = false);
        void Check(TrackedDownload trackedDownload);
        void ProcessFailed(TrackedDownload trackedDownload);
    }

    public class FailedDownloadService : IFailedDownloadService
    {
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;

        public FailedDownloadService(IHistoryService historyService,
                                     IEventAggregator eventAggregator)
        {
            _historyService = historyService;
            _eventAggregator = eventAggregator;
        }

        public void MarkAsFailed(int historyId, bool skipRedownload = false)
        {
            var history = _historyService.Get(historyId);

            var downloadId = history.DownloadId;

            if (downloadId.IsNullOrWhiteSpace())
            {
                PublishDownloadFailedEvent(history, new List<int> { history.AlbumId }, "Manually marked as failed", skipRedownload: skipRedownload);

                return;
            }

            var grabbedHistory = new List<EntityHistory>();

            // If the history item is a grabbed item (it should be, at least from the UI) add it as the first history item
            if (history.EventType == EntityHistoryEventType.Grabbed)
            {
                grabbedHistory.Add(history);
            }

            // Add any other history items for the download ID then filter out any duplicate history items.
            grabbedHistory.AddRange(GetGrabbedHistory(downloadId));
            grabbedHistory = grabbedHistory.DistinctBy(h => h.Id).ToList();

            PublishDownloadFailedEvent(history, GetAlbumIds(grabbedHistory), "Manually marked as failed");
        }

        public void MarkAsFailed(TrackedDownload trackedDownload, bool skipRedownload = false)
        {
            var history = GetGrabbedHistory(trackedDownload.DownloadItem.DownloadId);

            if (history.Any())
            {
                PublishDownloadFailedEvent(history.First(), GetAlbumIds(history), "Manually marked as failed", trackedDownload, skipRedownload: skipRedownload);
            }
        }

        public void Check(TrackedDownload trackedDownload)
        {
            // Only process tracked downloads that are still downloading or import is blocked (if they fail after attempting to be processed)
            if (trackedDownload.State != TrackedDownloadState.Downloading && trackedDownload.State != TrackedDownloadState.ImportBlocked)
            {
                return;
            }

            if (trackedDownload.DownloadItem.IsEncrypted ||
                trackedDownload.DownloadItem.Status == DownloadItemStatus.Failed)
            {
                var grabbedItems = GetGrabbedHistory(trackedDownload.DownloadItem.DownloadId);

                if (grabbedItems.Empty())
                {
                    trackedDownload.Warn("Download wasn't grabbed by Lidarr, skipping");
                    return;
                }

                trackedDownload.State = TrackedDownloadState.DownloadFailedPending;
            }
        }

        public void ProcessFailed(TrackedDownload trackedDownload)
        {
            if (trackedDownload.State != TrackedDownloadState.DownloadFailedPending)
            {
                return;
            }

            var grabbedItems = GetGrabbedHistory(trackedDownload.DownloadItem.DownloadId);

            if (grabbedItems.Empty())
            {
                return;
            }

            var failure = "Failed download detected";

            if (trackedDownload.DownloadItem.IsEncrypted)
            {
                failure = "Encrypted download detected";
            }
            else if (trackedDownload.DownloadItem.Status == DownloadItemStatus.Failed && trackedDownload.DownloadItem.Message.IsNotNullOrWhiteSpace())
            {
                failure = trackedDownload.DownloadItem.Message;
            }

            trackedDownload.State = TrackedDownloadState.DownloadFailed;
            PublishDownloadFailedEvent(grabbedItems.First(), GetAlbumIds(grabbedItems), failure, trackedDownload);
        }

        private void PublishDownloadFailedEvent(EntityHistory historyItem, List<int> albumIds, string message, TrackedDownload trackedDownload = null, bool skipRedownload = false)
        {
            Enum.TryParse(historyItem.Data.GetValueOrDefault(EntityHistory.RELEASE_SOURCE, ReleaseSourceType.Unknown.ToString()), out ReleaseSourceType releaseSource);

            var downloadFailedEvent = new DownloadFailedEvent
            {
                ArtistId = historyItem.ArtistId,
                AlbumIds = albumIds,
                Quality = historyItem.Quality,
                SourceTitle = historyItem.SourceTitle,
                DownloadClient = historyItem.Data.GetValueOrDefault(EntityHistory.DOWNLOAD_CLIENT),
                DownloadId = historyItem.DownloadId,
                Message = message,
                Data = historyItem.Data,
                TrackedDownload = trackedDownload,
                SkipRedownload = skipRedownload,
                ReleaseSource = releaseSource,
            };

            _eventAggregator.PublishEvent(downloadFailedEvent);
        }

        private List<int> GetAlbumIds(List<EntityHistory> historyItems)
        {
            return historyItems.Select(h => h.AlbumId).Distinct().ToList();
        }

        private List<EntityHistory> GetGrabbedHistory(string downloadId)
        {
            // Sort by date so items are always in the same order
            return _historyService.Find(downloadId, EntityHistoryEventType.Grabbed)
                .OrderByDescending(h => h.Date)
                .ToList();
        }
    }
}
