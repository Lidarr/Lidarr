using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class CloseTrackMatchSpecification : IImportDecisionEngineSpecification<LocalTrack>
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public CloseTrackMatchSpecification(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalTrack item, DownloadClientItem downloadClientItem)
        {
            var dist = item.Distance.NormalizedDistance();
            var reasons = item.Distance.Reasons;
            var threshold = _configService.TrackMatchThreshold / 100.0;

            if (dist > threshold)
            {
                _logger.Debug($"Track match is not close enough: {dist} vs {threshold} {reasons}. Skipping {item}");
                return Decision.Reject($"Track match is not close enough: {1 - dist:P1} vs {1 - threshold:P0} {reasons}");
            }

            _logger.Debug($"Track accepted: {dist} vs {threshold} {reasons}.");
            return Decision.Accept();
        }
    }
}
