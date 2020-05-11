using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
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
        void Process(TrackedDownload trackedDownload, bool ignoreWarnings = false);
    }

    public class CompletedDownloadService : ICompletedDownloadService
    {
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IHistoryService _historyService;
        private readonly IDownloadedTracksImportService _downloadedTracksImportService;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;
        private readonly IArtistService _artistService;

        public CompletedDownloadService(IConfigService configService,
                                        IEventAggregator eventAggregator,
                                        IHistoryService historyService,
                                        IDownloadedTracksImportService downloadedTracksImportService,
                                        IParsingService parsingService,
                                        IArtistService artistService,
                                        Logger logger)
        {
            _configService = configService;
            _eventAggregator = eventAggregator;
            _historyService = historyService;
            _downloadedTracksImportService = downloadedTracksImportService;
            _parsingService = parsingService;
            _logger = logger;
            _artistService = artistService;
        }

        public void Process(TrackedDownload trackedDownload, bool ignoreWarnings = false)
        {
            if (trackedDownload.DownloadItem.Status != DownloadItemStatus.Completed ||
                trackedDownload.RemoteAlbum == null)
            {
                return;
            }

            if (!ignoreWarnings)
            {
                var historyItem = _historyService.MostRecentForDownloadId(trackedDownload.DownloadItem.DownloadId);

                if (historyItem == null && trackedDownload.DownloadItem.Category.IsNullOrWhiteSpace())
                {
                    trackedDownload.Warn("Download wasn't grabbed by Lidarr and not in a category, Skipping.");
                    return;
                }

                var downloadItemOutputPath = trackedDownload.DownloadItem.OutputPath;

                if (downloadItemOutputPath.IsEmpty)
                {
                    trackedDownload.Warn("Download doesn't contain intermediate path, Skipping.");
                    return;
                }

                if ((OsInfo.IsWindows && !downloadItemOutputPath.IsWindowsPath) ||
                    (OsInfo.IsNotWindows && !downloadItemOutputPath.IsUnixPath))
                {
                    trackedDownload.Warn("[{0}] is not a valid local path. You may need a Remote Path Mapping.", downloadItemOutputPath);
                    return;
                }

                var artist = trackedDownload.RemoteAlbum.Artist;

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
            }

            Import(trackedDownload);
        }

        private void Import(TrackedDownload trackedDownload)
        {
            var outputPath = trackedDownload.DownloadItem.OutputPath.FullPath;
            var importResults = _downloadedTracksImportService.ProcessPath(outputPath, ImportMode.Auto, trackedDownload.RemoteAlbum.Artist, trackedDownload.DownloadItem);

            if (importResults.Empty())
            {
                trackedDownload.Warn("No files found are eligible for import in {0}", outputPath);
                return;
            }

            if (importResults.All(c => c.Result == ImportResultType.Imported)
                || importResults.Count(c => c.Result == ImportResultType.Imported) >= Math.Max(1, trackedDownload.RemoteAlbum.Albums.Sum(x => x.AlbumReleases.Value.Where(y => y.Monitored).Sum(z => z.TrackCount))))
            {
                trackedDownload.State = TrackedDownloadStage.Imported;
                _eventAggregator.PublishEvent(new DownloadCompletedEvent(trackedDownload));
                return;
            }

            if (importResults.Any(c => c.Result != ImportResultType.Imported))
            {
                trackedDownload.State = TrackedDownloadStage.ImportFailed;
                var statusMessages = importResults
                    .Where(v => v.Result != ImportResultType.Imported)
                    .Select(v => new TrackedDownloadStatusMessage(Path.GetFileName(v.ImportDecision.Item.Path), v.Errors))
                    .ToArray();

                trackedDownload.Warn(statusMessages);
                _eventAggregator.PublishEvent(new AlbumImportIncompleteEvent(trackedDownload));
            }
        }
    }
}
