using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    /// <summary>
    /// Rejects indexer releases whose title explicitly advertises a disc count (e.g. "2CD", "3 x CD",
    /// "4 Disc") that is excluded by an album's preferred medium count filter. Indexer results carry
    /// no track list, so this is the only release-side signal available before download. Titles
    /// without an explicit disc count are never rejected here; the filter is enforced on import.
    /// </summary>
    public class ReleaseMediumCountSpecification : IDecisionEngineSpecification
    {
        private static readonly Regex DiscCountRegex = new Regex(@"(?<![\w.])(?<count>\d{1,2})\s?-?\s?x?\s?(?:CD|Disc)s?(?![\w])",
                                                                 RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Logger _logger;

        public ReleaseMediumCountSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public static int? ParseDiscCount(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            var match = DiscCountRegex.Match(title);

            if (!match.Success)
            {
                return null;
            }

            var count = int.Parse(match.Groups["count"].Value);

            return count >= 2 ? count : null;
        }

        public virtual Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            var albums = subject.Albums?.Where(a => a.PreferredMediumCounts != null && a.PreferredMediumCounts.Any()).ToList();

            if (albums == null || !albums.Any())
            {
                return Decision.Accept();
            }

            var discCount = ParseDiscCount(subject.Release?.Title);

            if (!discCount.HasValue)
            {
                return Decision.Accept();
            }

            foreach (var album in albums)
            {
                if (!album.PreferredMediumCounts.Contains(discCount.Value))
                {
                    var wanted = string.Join(", ", album.PreferredMediumCounts.OrderBy(x => x));
                    _logger.Debug("Release {0} appears to be a {1} disc edition but album {2} is filtered to {3} media", subject.Release.Title, discCount, album, wanted);
                    return Decision.Reject("Release title indicates a {0} disc edition, album is filtered to {1} media", discCount.Value, wanted);
                }
            }

            return Decision.Accept();
        }
    }
}
