using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class MonitoredTrackSpecification : IImportDecisionEngineSpecification<LocalTrack>
    {
        private readonly Logger _logger;

        public MonitoredTrackSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalTrack item, DownloadClientItem downloadClientItem)
        {
            if (item.ExistingFile)
            {
                return Decision.Accept();
            }

            var targetRecordingIds = downloadClientItem?.TargetRecordingIds;

            if (targetRecordingIds?.Any() == true)
            {
                if (item.Tracks.Any(track => targetRecordingIds.Contains(track.ForeignRecordingId)))
                {
                    return Decision.Accept();
                }

                _logger.Debug("Track {0} was not targeted by this download", item);
                return Decision.Reject("Track was not targeted by this download");
            }

            if (item.Tracks.Any(track => track.Monitored))
            {
                return Decision.Accept();
            }

            _logger.Debug("Track {0} is not monitored", item);
            return Decision.Reject("Track is not monitored");
        }
    }
}
