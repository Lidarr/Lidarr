using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class AlreadyImportedSpecification : IImportDecisionEngineSpecification<LocalAlbumRelease>
    {
        private readonly IHistoryService _historyService;
        private readonly Logger _logger;

        public AlreadyImportedSpecification(IHistoryService historyService,
                                            Logger logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Database;

        public Decision IsSatisfiedBy(LocalAlbumRelease localAlbumRelease, DownloadClientItem downloadClientItem)
        {
            if (downloadClientItem == null)
            {
                _logger.Debug("No download client information is available, skipping");
                return Decision.Accept();
            }

            var albumRelease = localAlbumRelease.AlbumRelease;

            if (!albumRelease.Tracks.Value.Any(x => x.HasFile))
            {
                _logger.Debug("Skipping already imported check for album without files");
                return Decision.Accept();
            }

            var albumHistory = _historyService.GetByAlbum(albumRelease.AlbumId, null);
            var lastImported = albumHistory.FirstOrDefault(h =>
                h.DownloadId == downloadClientItem.DownloadId &&
                h.EventType == EntityHistoryEventType.DownloadImported);
            var lastGrabbed = albumHistory.FirstOrDefault(h =>
                h.DownloadId == downloadClientItem.DownloadId && h.EventType == EntityHistoryEventType.Grabbed);

            if (lastImported == null)
            {
                _logger.Trace("Album has not been imported");
                return Decision.Accept();
            }

            if (lastGrabbed != null)
            {
                // If the release was grabbed again after importing don't reject it
                if (lastGrabbed.Date.After(lastImported.Date))
                {
                    _logger.Trace("Album was grabbed again after importing");
                    return Decision.Accept();
                }

                // If the release was imported after the last grab reject it
                if (lastImported.Date.After(lastGrabbed.Date))
                {
                    _logger.Debug("Album previously imported at {0}", lastImported.Date);
                    return Decision.Reject("Album already imported at {0}", lastImported.Date.ToLocalTime());
                }
            }

            return Decision.Accept();
        }
    }
}
