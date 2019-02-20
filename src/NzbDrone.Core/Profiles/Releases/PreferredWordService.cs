using NLog;
using NzbDrone.Core.Music;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Profiles.Releases
{
    public interface IPreferredWordService
    {
        int Calculate(Artist artist, string title);
        List<string> GetMatchingPreferredWords(Artist artist, string title, bool isRenaming);
    }

    public class PreferredWordService : IPreferredWordService
    {
        private readonly IReleaseProfileService _releaseProfileService;
        private readonly ITermMatcher _termMatcher;
        private readonly Logger _logger;

        public PreferredWordService(IReleaseProfileService releaseProfileService, ITermMatcher termMatcher, Logger logger)
        {
            _releaseProfileService = releaseProfileService;
            _termMatcher = termMatcher;
            _logger = logger;
        }

        public int Calculate(Artist series, string title)
        {
            _logger.Trace("Calculating preferred word score for '{0}'", title);

            var matchingPairs = GetMatchingPairs(series, title, false);
            var score = matchingPairs.Sum(p => p.Value);

            _logger.Trace("Calculated preferred word score for '{0}': {1}", title, score);

            return score;
        }

        public List<string> GetMatchingPreferredWords(Artist artist, string title, bool isRenaming)
        {
            var matchingPairs = GetMatchingPairs(artist, title, isRenaming);

            return matchingPairs.OrderByDescending(p => p.Value)
                                .Select(p => p.Key)
                                .ToList();
        }

        private List<KeyValuePair<string, int>> GetMatchingPairs(Artist artist, string title, bool isRenaming)
        {
            var releaseProfiles = _releaseProfileService.AllForTags(artist.Tags);
            var result = new List<KeyValuePair<string, int>>();

            _logger.Trace("Calculating preferred word score for '{0}'", title);

            foreach (var releaseProfile in releaseProfiles)
            {
                if (isRenaming && !releaseProfile.IncludePreferredWhenRenaming)
                {
                    continue;
                }

                foreach (var preferredPair in releaseProfile.Preferred)
                {
                    var term = preferredPair.Key;

                    if (_termMatcher.IsMatch(term, title))
                    {
                        result.Add(preferredPair);
                    }
                }
            }

            return result;
        }
    }
}
