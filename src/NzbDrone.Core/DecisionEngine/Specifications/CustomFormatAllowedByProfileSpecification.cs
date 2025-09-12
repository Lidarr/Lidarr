using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class CustomFormatAllowedbyProfileSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;
        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public CustomFormatAllowedbyProfileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public virtual Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            var minScore = subject.Artist.QualityProfile.Value.MinFormatScore;
            var score = subject.CustomFormatScore;

            if (score < minScore)
            {
                return Decision.Reject("Custom Formats {0} have score {1} below Artist profile minimum {2}", subject.CustomFormats.ConcatToString(), score, minScore);
            }

            _logger.Trace("Custom Format Score of {0} [{1}] above Artist profile minimum {2}", score, subject.CustomFormats.ConcatToString(), minScore);

            return Decision.Accept();
        }
    }
}
