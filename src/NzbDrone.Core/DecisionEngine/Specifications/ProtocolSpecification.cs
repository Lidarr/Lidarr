using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class ProtocolSpecification : IDecisionEngineSpecification
    {
        private readonly IDelayProfileService _delayProfileService;
        private readonly Logger _logger;

        public ProtocolSpecification(IDelayProfileService delayProfileService,
                                     Logger logger)
        {
            _delayProfileService = delayProfileService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            var protocol = subject.Release.DownloadProtocol;
            var delayProfile = _delayProfileService.BestForTags(subject.Artist.Tags);

            if (!delayProfile.IsAllowedProtocol(subject.Release.DownloadProtocol))
            {
                _logger.Debug("[{0}] {1} is not enabled for this artist", subject.Release.Title, protocol);
                return Decision.Reject($"{protocol} is not enabled for this artist");
            }

            return Decision.Accept();
        }
    }
}
