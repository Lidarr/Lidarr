using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class SingleAlbumSearchMatchSpecification : IDecisionEngineSpecification
    {
        // Words that only name an edition of the same recordings. Words that name other
        // recordings, such as live, acoustic, instrumental or remix, are left out on purpose.
        private static readonly HashSet<string> EditionWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "anniversary", "bonus", "collectors", "deluxe", "digital", "edition", "expanded", "explicit",
            "international", "limited", "mixtape", "premium", "reissue", "remaster", "remastered",
            "special", "super", "track", "tracks", "version"
        };

        private static readonly Regex WordRegex = new Regex(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

        // A year or an ordinal, as in "Remastered 2011" or "10th Anniversary".
        private static readonly Regex NumberRegex = new Regex(@"^((19|20)\d{2}|\d+(st|nd|rd|th))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Logger _logger;

        public SingleAlbumSearchMatchSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteAlbum remoteAlbum, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            var singleAlbumSpec = searchCriteria as AlbumSearchCriteria;
            if (singleAlbumSpec == null)
            {
                return Decision.Accept();
            }

            if (Parser.Parser.CleanArtistName(singleAlbumSpec.AlbumTitle) != Parser.Parser.CleanArtistName(remoteAlbum.ParsedAlbumInfo.AlbumTitle))
            {
                if (!TitlesWithoutEdition(singleAlbumSpec.AlbumTitle).Overlaps(TitlesWithoutEdition(remoteAlbum.ParsedAlbumInfo.AlbumTitle)))
                {
                    _logger.Debug("Album does not match searched album title, skipping.");
                    return Decision.Reject("Wrong album");
                }

                _logger.Debug("Album matches searched album title apart from its edition.");
            }

            if (!remoteAlbum.ParsedAlbumInfo.AlbumTitle.Any())
            {
                _logger.Debug("Full discography result during single album search, skipping.");
                return Decision.Reject("Full artist pack");
            }

            return Decision.Accept();
        }

        private static HashSet<string> TitlesWithoutEdition(string title)
        {
            var words = WordRegex.Matches(title.Replace("'", string.Empty).Replace("’", string.Empty))
                .Select(m => m.Value)
                .ToList();
            var titles = new HashSet<string> { Parser.Parser.CleanArtistName(title) };
            var edition = false;

            for (var end = words.Count - 1; end > 0; end--)
            {
                var word = words[end];

                if (EditionWords.Contains(word))
                {
                    edition = true;
                }
                else if (!NumberRegex.IsMatch(word))
                {
                    break;
                }

                if (edition)
                {
                    titles.Add(Parser.Parser.CleanArtistName(string.Join(" ", words.Take(end))));
                }
            }

            return titles;
        }
    }
}
