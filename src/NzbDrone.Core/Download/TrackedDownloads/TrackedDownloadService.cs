using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadService
    {
        TrackedDownload Find(string downloadId);
        void StopTracking(string downloadId);
        void StopTracking(List<string> downloadIds);
        TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem);
        List<TrackedDownload> GetTrackedDownloads();
        void UpdateTrackable(List<TrackedDownload> trackedDownloads);
    }

    public class TrackedDownloadService : ITrackedDownloadService,
                                          IHandle<AlbumInfoRefreshedEvent>,
                                          IHandle<AlbumDeletedEvent>,
                                          IHandle<ArtistAddedEvent>,
                                          IHandle<ArtistsDeletedEvent>
    {
        private readonly IParsingService _parsingService;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDownloadHistoryService _downloadHistoryService;
        private readonly IRemoteAlbumAggregationService _aggregationService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly Logger _logger;
        private readonly ICached<TrackedDownload> _cache;

        public TrackedDownloadService(IParsingService parsingService,
                                      ICacheManager cacheManager,
                                      IHistoryService historyService,
                                      ICustomFormatCalculationService formatCalculator,
                                      IEventAggregator eventAggregator,
                                      IDownloadHistoryService downloadHistoryService,
                                      IRemoteAlbumAggregationService aggregationService,
                                      Logger logger)
        {
            _parsingService = parsingService;
            _historyService = historyService;
            _cache = cacheManager.GetCache<TrackedDownload>(GetType());
            _formatCalculator = formatCalculator;
            _eventAggregator = eventAggregator;
            _downloadHistoryService = downloadHistoryService;
            _aggregationService = aggregationService;
            _logger = logger;
        }

        public TrackedDownload Find(string downloadId)
        {
            return _cache.Find(downloadId);
        }

        private void UpdateCachedItem(TrackedDownload trackedDownload)
        {
            var parsedAlbumInfo = Parser.Parser.ParseAlbumTitle(trackedDownload.DownloadItem.Title);

            trackedDownload.RemoteAlbum = parsedAlbumInfo == null ? null : _parsingService.Map(parsedAlbumInfo);

            _aggregationService.Augment(trackedDownload.RemoteAlbum);
        }

        public void StopTracking(string downloadId)
        {
            var trackedDownload = _cache.Find(downloadId);

            _cache.Remove(downloadId);
            _eventAggregator.PublishEvent(new TrackedDownloadsRemovedEvent(new List<TrackedDownload> { trackedDownload }));
        }

        public void StopTracking(List<string> downloadIds)
        {
            var trackedDownloads = new List<TrackedDownload>();

            foreach (var downloadId in downloadIds)
            {
                var trackedDownload = _cache.Find(downloadId);
                _cache.Remove(downloadId);
                trackedDownloads.Add(trackedDownload);
            }

            _eventAggregator.PublishEvent(new TrackedDownloadsRemovedEvent(trackedDownloads));
        }

        public TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem)
        {
            var existingItem = Find(downloadItem.DownloadId);

            if (existingItem != null && existingItem.State != TrackedDownloadState.Downloading)
            {
                LogItemChange(existingItem, existingItem.DownloadItem, downloadItem);

                existingItem.DownloadItem = downloadItem;
                existingItem.IsTrackable = true;

                return existingItem;
            }

            var trackedDownload = new TrackedDownload
            {
                DownloadClient = downloadClient.Id,
                DownloadItem = downloadItem,
                Protocol = downloadClient.Protocol,
                IsTrackable = true
            };

            try
            {
                var historyItems = _historyService.FindByDownloadId(downloadItem.DownloadId)
                    .OrderByDescending(h => h.Date)
                    .ToList();

                // TODO: Create release info from history and use that here, so we don't loose indexer flags!
                var parsedAlbumInfo = Parser.Parser.ParseAlbumTitle(trackedDownload.DownloadItem.Title);

                if (parsedAlbumInfo != null)
                {
                    trackedDownload.RemoteAlbum = _parsingService.Map(parsedAlbumInfo);
                }

                var downloadHistory = _downloadHistoryService.GetLatestDownloadHistoryItem(downloadItem.DownloadId);

                if (downloadHistory != null)
                {
                    var state = GetStateFromHistory(downloadHistory.EventType);
                    trackedDownload.State = state;

                    if (downloadHistory.EventType == DownloadHistoryEventType.DownloadImportIncomplete)
                    {
                        var messages = Json.Deserialize<List<TrackedDownloadStatusMessage>>(downloadHistory.Data["statusMessages"]).ToArray();
                        trackedDownload.Warn(messages);
                    }
                }

                if (historyItems.Any())
                {
                    var firstHistoryItem = historyItems.First();
                    var grabbedEvent = historyItems.FirstOrDefault(v => v.EventType == EntityHistoryEventType.Grabbed);

                    trackedDownload.Indexer = grabbedEvent?.Data?.GetValueOrDefault("indexer");
                    trackedDownload.Added = grabbedEvent?.Date;

                    if (parsedAlbumInfo == null ||
                        trackedDownload.RemoteAlbum?.Artist == null ||
                        trackedDownload.RemoteAlbum.Albums.Empty())
                    {
                        // Try parsing the original source title and if that fails, try parsing it as a special
                        var historyArtist = firstHistoryItem.Artist;
                        var historyAlbums = new List<Album> { firstHistoryItem.Album };

                        parsedAlbumInfo = Parser.Parser.ParseAlbumTitle(firstHistoryItem.SourceTitle);

                        if (parsedAlbumInfo != null)
                        {
                            trackedDownload.RemoteAlbum = _parsingService.Map(parsedAlbumInfo,
                                firstHistoryItem.ArtistId,
                                historyItems.Where(v => v.EventType == EntityHistoryEventType.Grabbed).Select(h => h.AlbumId)
                                    .Distinct());
                        }
                        else
                        {
                            parsedAlbumInfo =
                                Parser.Parser.ParseAlbumTitleWithSearchCriteria(firstHistoryItem.SourceTitle,
                                    historyArtist,
                                    historyAlbums);

                            if (parsedAlbumInfo != null)
                            {
                                trackedDownload.RemoteAlbum = _parsingService.Map(parsedAlbumInfo,
                                    firstHistoryItem.ArtistId,
                                    historyItems.Where(v => v.EventType == EntityHistoryEventType.Grabbed).Select(h => h.AlbumId)
                                        .Distinct());
                            }
                        }
                    }

                    if (trackedDownload.RemoteAlbum != null &&
                        Enum.TryParse(grabbedEvent?.Data?.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                    {
                        trackedDownload.RemoteAlbum.Release ??= new ReleaseInfo();
                        trackedDownload.RemoteAlbum.Release.IndexerFlags = flags;
                    }
                }

                if (trackedDownload.RemoteAlbum != null)
                {
                    _aggregationService.Augment(trackedDownload.RemoteAlbum);

                    // Calculate custom formats
                    trackedDownload.RemoteAlbum.CustomFormats = _formatCalculator.ParseCustomFormat(trackedDownload.RemoteAlbum, downloadItem.TotalSize);
                }

                // Track it so it can be displayed in the queue even though we can't determine which artist it is for
                if (trackedDownload.RemoteAlbum == null)
                {
                    _logger.Trace("No Album found for download '{0}'", trackedDownload.DownloadItem.Title);
                }
            }
            catch (Exception e)
            {
                _logger.Debug(e, "Failed to find album for " + downloadItem.Title);
                return null;
            }

            LogItemChange(trackedDownload, existingItem?.DownloadItem, trackedDownload.DownloadItem);

            _cache.Set(trackedDownload.DownloadItem.DownloadId, trackedDownload);
            return trackedDownload;
        }

        public List<TrackedDownload> GetTrackedDownloads()
        {
            return _cache.Values.ToList();
        }

        public void UpdateTrackable(List<TrackedDownload> trackedDownloads)
        {
            var untrackable = GetTrackedDownloads().ExceptBy(t => t.DownloadItem.DownloadId, trackedDownloads, t => t.DownloadItem.DownloadId, StringComparer.CurrentCulture).ToList();

            foreach (var trackedDownload in untrackable)
            {
                trackedDownload.IsTrackable = false;
            }
        }

        private static TrackedDownloadState GetStateFromHistory(DownloadHistoryEventType eventType)
        {
            switch (eventType)
            {
                case DownloadHistoryEventType.DownloadImportIncomplete:
                    return TrackedDownloadState.ImportFailed;
                case DownloadHistoryEventType.DownloadImported:
                    return TrackedDownloadState.Imported;
                case DownloadHistoryEventType.DownloadFailed:
                    return TrackedDownloadState.DownloadFailed;
                case DownloadHistoryEventType.DownloadIgnored:
                    return TrackedDownloadState.Ignored;
                default:
                    return TrackedDownloadState.Downloading;
            }
        }

        private void LogItemChange(TrackedDownload trackedDownload, DownloadClientItem existingItem, DownloadClientItem downloadItem)
        {
            if (existingItem == null ||
                existingItem.Status != downloadItem.Status ||
                existingItem.CanBeRemoved != downloadItem.CanBeRemoved ||
                existingItem.CanMoveFiles != downloadItem.CanMoveFiles)
            {
                _logger.Debug("Tracking '{0}:{1}': ClientState={2}{3} LidarrStage={4} Album='{5}' OutputPath={6}.",
                    downloadItem.DownloadClientInfo.Name,
                    downloadItem.Title,
                    downloadItem.Status,
                    downloadItem.CanBeRemoved ? "" : downloadItem.CanMoveFiles ? " (busy)" : " (readonly)",
                    trackedDownload.State,
                    trackedDownload.RemoteAlbum?.ParsedAlbumInfo,
                    downloadItem.OutputPath);
            }
        }

        public void Handle(AlbumInfoRefreshedEvent message)
        {
            var needsToUpdate = false;

            foreach (var album in message.Removed)
            {
                var cachedItems = _cache.Values.Where(t =>
                                            t.RemoteAlbum?.Albums != null &&
                                            t.RemoteAlbum.Albums.Any(e => e.Id == album.Id))
                                        .ToList();

                if (cachedItems.Any())
                {
                    needsToUpdate = true;
                }

                cachedItems.ForEach(UpdateCachedItem);
            }

            if (needsToUpdate)
            {
                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }

        public void Handle(AlbumDeletedEvent message)
        {
            var cachedItems = _cache.Values
                .Where(t =>
                    t.RemoteAlbum?.Albums != null &&
                    t.RemoteAlbum.Albums.Any(a => a.Id == message.Album.Id || a.ForeignAlbumId == message.Album.ForeignAlbumId))
                .ToList();

            if (cachedItems.Any())
            {
                cachedItems.ForEach(UpdateCachedItem);

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }

        public void Handle(ArtistAddedEvent message)
        {
            var cachedItems = _cache.Values
                .Where(t =>
                    t.RemoteAlbum?.Artist == null ||
                    message.Artist?.ForeignArtistId == t.RemoteAlbum.Artist.ForeignArtistId)
                .ToList();

            if (cachedItems.Any())
            {
                cachedItems.ForEach(UpdateCachedItem);

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }

        public void Handle(ArtistsDeletedEvent message)
        {
            var cachedItems = _cache.Values
                .Where(t =>
                    t.RemoteAlbum?.Artist != null &&
                    message.Artists.Any(a => a.Id == t.RemoteAlbum.Artist.Id || a.ForeignArtistId == t.RemoteAlbum.Artist.ForeignArtistId))
                .ToList();

            if (cachedItems.Any())
            {
                cachedItems.ForEach(UpdateCachedItem);

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }
    }
}
