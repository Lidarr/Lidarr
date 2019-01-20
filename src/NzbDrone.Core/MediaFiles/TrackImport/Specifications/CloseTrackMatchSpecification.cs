using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class CloseTrackMatchSpecification : IImportDecisionEngineSpecification<LocalTrack>
    {
        private readonly Logger _logger;
        private const double _threshold = 0.4;

        public CloseTrackMatchSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalTrack localTrack)
        {
            var dist = localTrack.Distance.NormalizedDistance();
            var reasons = localTrack.Distance.Reasons;

            if (dist > _threshold)
            {
                _logger.Debug($"Track match is not close enough: {dist} vs {_threshold} [{reasons}]. Skipping {localTrack}");
                return Decision.Reject("Track match not close enough");
            }

            _logger.Debug($"Track accepted: {dist} vs {_threshold} [{reasons}].");
            return Decision.Accept();
        }
    }
}
