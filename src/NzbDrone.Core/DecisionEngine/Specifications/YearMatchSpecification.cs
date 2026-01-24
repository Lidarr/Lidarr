using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class YearMatchSpecification : IDecisionEngineSpecification
    {
        private readonly IAlbumYearMatcher _yearMatcher;
        private readonly Logger _logger;

        public YearMatchSpecification(IAlbumYearMatcher yearMatcher, Logger logger)
        {
            _yearMatcher = yearMatcher;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            var parsedYear = subject.ParsedAlbumInfo?.ReleaseYear;

            if (!parsedYear.HasValue || subject.Albums == null || !subject.Albums.Any())
            {
                return Decision.Accept();
            }

            foreach (var album in subject.Albums)
            {
                var result = _yearMatcher.Match(album, parsedYear);

                if (!result.IsMatch)
                {
                    _logger.Debug("Rejecting release {0}: {1}", subject.Release.Title, result.RejectionReason);
                    return Decision.Reject(result.RejectionReason);
                }

                if (result.YearDifference > AlbumYearMatchingOptions.FuzzyMatchMaxYearDiff)
                {
                    _logger.Debug("Release {0} has year difference of {1} years, accepting with warning", subject.Release.Title, result.YearDifference);
                }
            }

            return Decision.Accept();
        }
    }
}
