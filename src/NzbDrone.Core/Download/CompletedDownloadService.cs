using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.TrackImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Download
{
    public interface ICompletedDownloadService
    {
        void Check(TrackedDownload trackedDownload);
        void Import(TrackedDownload trackedDownload);
        bool VerifyImport(TrackedDownload trackedDownload, List<ImportResult> importResults);
    }

    public class CompletedDownloadService : ICompletedDownloadService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IHistoryService _historyService;
        private readonly IDownloadedTracksImportService _downloadedTracksImportService;
        private readonly IArtistService _artistService;
        private readonly IProvideImportItemService _provideImportItemService;
        private readonly IParsingService _parsingService;
        private readonly ITrackedDownloadAlreadyImported _trackedDownloadAlreadyImported;
        private readonly Logger _logger;

        public CompletedDownloadService(IEventAggregator eventAggregator,
                                        IHistoryService historyService,
                                        IProvideImportItemService provideImportItemService,
                                        IDownloadedTracksImportService downloadedTracksImportService,
                                        IArtistService artistService,
                                        IParsingService parsingService,
                                        ITrackedDownloadAlreadyImported trackedDownloadAlreadyImported,
                                        Logger logger)
        {
            _eventAggregator = eventAggregator;
            _historyService = historyService;
            _provideImportItemService = provideImportItemService;
            _downloadedTracksImportService = downloadedTracksImportService;
            _artistService = artistService;
            _parsingService = parsingService;
            _trackedDownloadAlreadyImported = trackedDownloadAlreadyImported;
            _logger = logger;
        }

        public void Check(TrackedDownload trackedDownload)
        {
            if (trackedDownload.DownloadItem.Status != DownloadItemStatus.Completed)
            {
                return;
            }

            SetImportItem(trackedDownload);

            // Only process tracked downloads that are still downloading
            if (trackedDownload.State != TrackedDownloadState.Downloading)
            {
                return;
            }

            var historyItem = _historyService.MostRecentForDownloadId(trackedDownload.DownloadItem.DownloadId);

            if (historyItem == null && trackedDownload.DownloadItem.Category.IsNullOrWhiteSpace())
            {
                trackedDownload.Warn("Download wasn't grabbed by Lidarr and not in a category, Skipping.");
                return;
            }

            if (!ValidatePath(trackedDownload))
            {
                return;
            }

            var artist = _parsingService.GetArtist(trackedDownload.DownloadItem.Title);

            if (artist == null)
            {
                if (historyItem != null)
                {
                    artist = _artistService.GetArtist(historyItem.ArtistId);
                }

                if (artist == null)
                {
                    trackedDownload.Warn("Artist name mismatch, automatic import is not possible.");
                    return;
                }
            }

            trackedDownload.State = TrackedDownloadState.ImportPending;
        }

        public void Import(TrackedDownload trackedDownload)
        {
            SetImportItem(trackedDownload);

            if (!ValidatePath(trackedDownload))
            {
                return;
            }

            if (trackedDownload.RemoteAlbum == null)
            {
                trackedDownload.Warn("Unable to parse download, automatic import is not possible.");
                return;
            }

            trackedDownload.State = TrackedDownloadState.Importing;

            var outputPath = trackedDownload.ImportItem.OutputPath.FullPath;
            var importResults = _downloadedTracksImportService.ProcessPath(outputPath, ImportMode.Auto, trackedDownload.RemoteAlbum.Artist, trackedDownload.ImportItem);

            if (VerifyImport(trackedDownload, importResults))
            {
                return;
            }

            trackedDownload.State = TrackedDownloadState.ImportPending;

            if (importResults.Empty())
            {
                trackedDownload.Warn("No files found are eligible for import in {0}", outputPath);

                return;
            }

            if (importResults.Count == 1)
            {
                var firstResult = importResults.First();

                if (firstResult.Result == ImportResultType.Rejected && firstResult.ImportDecision.Item == null)
                {
                    trackedDownload.Warn(new TrackedDownloadStatusMessage(firstResult.Errors.First(), new List<string>()));

                    return;
                }
            }

            var statusMessages = new List<TrackedDownloadStatusMessage>
                                 {
                                    new TrackedDownloadStatusMessage("One or more albums expected in this release were not imported or missing", new List<string>())
                                 };

            if (importResults.Any(c => c.Result != ImportResultType.Imported))
            {
                // Mark as failed to prevent further attempts at processing
                trackedDownload.State = TrackedDownloadState.ImportFailed;

                statusMessages.AddRange(
                    importResults
                        .Where(v => v.Result != ImportResultType.Imported && v.ImportDecision.Item != null)
                        .OrderBy(v => v.ImportDecision.Item.Path)
                        .Select(v =>
                            new TrackedDownloadStatusMessage(Path.GetFileName(v.ImportDecision.Item.Path),
                                v.Errors)));

                if (statusMessages.Any())
                {
                    trackedDownload.Warn(statusMessages.ToArray());
                }

                // Publish event to notify Album was imported incompelte
                _eventAggregator.PublishEvent(new AlbumImportIncompleteEvent(trackedDownload));
                return;
            }
        }

        public bool VerifyImport(TrackedDownload trackedDownload, List<ImportResult> importResults)
        {
            var allTracksImported = importResults.All(c => c.Result == ImportResultType.Imported) ||
                importResults.Count(c => c.Result == ImportResultType.Imported) >=
                Math.Max(1, trackedDownload.RemoteAlbum.Albums.Sum(x => x.AlbumReleases.Value.Where(y => y.Monitored).Sum(z => z.TrackCount)));

            if (allTracksImported)
            {
                _logger.Debug("All albums were imported for {0}", trackedDownload.DownloadItem.Title);
                trackedDownload.State = TrackedDownloadState.Imported;

                _eventAggregator.PublishEvent(new DownloadCompletedEvent(trackedDownload, trackedDownload.RemoteAlbum.Artist.Id));
                return true;
            }

            // Double check if all episodes were imported by checking the history if at least one
            // file was imported. This will allow the decision engine to reject already imported
            // episode files and still mark the download complete when all files are imported.

            // EDGE CASE: This process relies on EpisodeIds being consistent between executions, if a series is updated
            // and an episode is removed, but later comes back with a different ID then Sonarr will treat it as incomplete.
            // Since imports should be relatively fast and these types of data changes are infrequent this should be quite
            // safe, but commenting for future benefit.
            var atLeastOneEpisodeImported = importResults.Any(c => c.Result == ImportResultType.Imported);

            var historyItems = _historyService.FindByDownloadId(trackedDownload.DownloadItem.DownloadId)
                                              .OrderByDescending(h => h.Date)
                                              .ToList();

            var allTracksImportedInHistory = _trackedDownloadAlreadyImported.IsImported(trackedDownload, historyItems);

            if (allTracksImportedInHistory)
            {
                // Log different error messages depending on the circumstances, but treat both as fully imported, because that's the reality.
                // The second message shouldn't be logged in most cases, but continued reporting would indicate an ongoing issue.
                if (atLeastOneEpisodeImported)
                {
                    _logger.Debug("All albums were imported in history for {0}", trackedDownload.DownloadItem.Title);
                }
                else
                {
                    _logger.ForDebugEvent()
                           .Message("No albums were just imported, but all albums were previously imported, possible issue with download history.")
                           .Property("ArtistId", trackedDownload.RemoteAlbum.Artist.Id)
                           .Property("DownloadId", trackedDownload.DownloadItem.DownloadId)
                           .Property("Title", trackedDownload.DownloadItem.Title)
                           .Property("Path", trackedDownload.DownloadItem.OutputPath.ToString())
                           .WriteSentryWarn("DownloadHistoryIncomplete")
                           .Log();
                }

                trackedDownload.State = TrackedDownloadState.Imported;
                _eventAggregator.PublishEvent(new DownloadCompletedEvent(trackedDownload, trackedDownload.RemoteAlbum.Artist.Id));

                return true;
            }

            _logger.Debug("Not all albums have been imported for {0}", trackedDownload.DownloadItem.Title);
            return false;
        }

        private void SetImportItem(TrackedDownload trackedDownload)
        {
            trackedDownload.ImportItem = _provideImportItemService.ProvideImportItem(trackedDownload.DownloadItem, trackedDownload.ImportItem);
        }

        private bool ValidatePath(TrackedDownload trackedDownload)
        {
            var downloadItemOutputPath = trackedDownload.ImportItem.OutputPath;

            if (downloadItemOutputPath.IsEmpty)
            {
                trackedDownload.Warn("Download doesn't contain intermediate path, Skipping.");
                return false;
            }

            if ((OsInfo.IsWindows && !downloadItemOutputPath.IsWindowsPath) ||
                (OsInfo.IsNotWindows && !downloadItemOutputPath.IsUnixPath))
            {
                trackedDownload.Warn("[{0}] is not a valid local path. You may need a Remote Path Mapping.", downloadItemOutputPath);
                return false;
            }

            return true;
        }
    }
}
