using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.History;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadAlreadyImported
    {
        bool IsImported(TrackedDownload trackedDownload, List<EntityHistory> historyItems);
    }

    public class TrackedDownloadAlreadyImported : ITrackedDownloadAlreadyImported
    {
        private readonly Logger _logger;
        private readonly ITrackService _trackService;

        public TrackedDownloadAlreadyImported(ITrackService trackService, Logger logger)
        {
            _trackService = trackService;
            _logger = logger;
        }

        public bool IsImported(TrackedDownload trackedDownload, List<EntityHistory> historyItems)
        {
            _logger.Trace("Checking if all items for '{0}' have been imported", trackedDownload.DownloadItem.Title);

            if (historyItems.Empty())
            {
                _logger.Trace("No history for {0}", trackedDownload.DownloadItem.Title);
                return false;
            }

            if (trackedDownload.RemoteAlbum == null)
            {
                return true;
            }

            if (trackedDownload.RemoteAlbum.TargetRecordingIds?.Any() == true || trackedDownload.DownloadItem.IsFullAlbumRequest)
            {
                var requiredRecordingIds = trackedDownload.RemoteAlbum.TargetRecordingIds;
                if (requiredRecordingIds?.Any() != true)
                {
                    if (trackedDownload.RemoteAlbum.Albums?.Any() != true)
                    {
                        return false;
                    }

                    if (trackedDownload.RemoteAlbum.Albums.All(album => historyItems.Any(item =>
                        item.AlbumId == album.Id && item.EventType == EntityHistoryEventType.DownloadImported)))
                    {
                        return true;
                    }

                    requiredRecordingIds = trackedDownload.RemoteAlbum.Albums
                        .SelectMany(album => album.AlbumReleases?.Value ?? new List<AlbumRelease>())
                        .Where(release => release.Monitored)
                        .SelectMany(release => release.Tracks?.Value ?? new List<Track>())
                        .Select(track => track.ForeignRecordingId)
                        .Where(id => id.IsNotNullOrWhiteSpace()).Distinct().ToList();
                    if (!requiredRecordingIds.Any())
                    {
                        return false;
                    }
                }

                var importedHistory = historyItems.Where(item => item.EventType == EntityHistoryEventType.TrackFileImported).ToList();
                var importedRecordingIds = importedHistory.Select(GetRecordingId)
                    .Where(id => id.IsNotNullOrWhiteSpace())
                    .ToHashSet();
                var legacyTrackIds = importedHistory.Where(item => GetRecordingId(item).IsNullOrWhiteSpace())
                    .Select(item => item.TrackId).Distinct().ToList();

                if (legacyTrackIds.Any())
                {
                    importedRecordingIds.UnionWith(_trackService.GetTracks(legacyTrackIds).Select(track => track.ForeignRecordingId));
                }

                return requiredRecordingIds.All(importedRecordingIds.Contains);
            }

            if (trackedDownload.RemoteAlbum.Albums == null)
            {
                return true;
            }

            var allAlbumsImportedInHistory = trackedDownload.RemoteAlbum.Albums.All(album =>
            {
                var lastHistoryItem = historyItems.FirstOrDefault(h => h.AlbumId == album.Id);

                if (lastHistoryItem == null)
                {
                    _logger.Trace($"No history for album: {album}");
                    return false;
                }

                _logger.Trace($"Last event for album: {album} is: {lastHistoryItem.EventType}");

                return new[] { EntityHistoryEventType.DownloadImported, EntityHistoryEventType.TrackFileImported }.Contains(lastHistoryItem.EventType);
            });

            _logger.Trace("All albums for '{0}' have been imported: {1}", trackedDownload.DownloadItem.Title, allAlbumsImportedInHistory);

            return allAlbumsImportedInHistory;
        }

        private static string GetRecordingId(EntityHistory item)
        {
            return item.Data.GetValueOrDefault("recordingId") ?? item.Data.GetValueOrDefault("RecordingId");
        }
    }
}
