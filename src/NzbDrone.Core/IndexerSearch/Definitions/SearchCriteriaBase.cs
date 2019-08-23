using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public abstract class SearchCriteriaBase
    {
        private static readonly Regex SpecialCharacter = new Regex(@"[`'’.]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NonWord = new Regex(@"[\W]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BeginningThe = new Regex(@"^the\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex IsAllWord = new Regex(@"^[\sA-Za-z0-9_`'’.&:-]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public virtual bool MonitoredEpisodesOnly { get; set; }
        public virtual bool UserInvokedSearch { get; set; }
        public virtual bool InteractiveSearch { get; set; }

        public Artist Artist { get; set; }
        public List<Album> Albums { get; set; }
        public List<Track> Tracks { get; set; }

        public string ArtistQuery => GetQueryTitle(Artist.Name);
        public List<string> ArtistQueries => OrderQueries(Artist.Metadata.Value.Name, Artist.Metadata.Value.Aliases);

        protected List<string> OrderQueries(string title, List<string> aliases)
        {
            var result = new List<string>();

            // find the primary search term.  This will be title if there are no special characters in the title,
            // otherwise the first alias with no special characters
            if (IsAllWord.IsMatch(title))
            {
                result.Add(title);
            }
            else
            {
                result.Add(aliases.FirstOrDefault(x => IsAllWord.IsMatch(x)) ?? title);
                result.Add(title);
            }

            // insert remaining aliases
            result.AddRange(aliases.Except(result));

            return result;
        }

        protected List<List<string>> GetQueryTiers(List<string> titles)
        {
            var result = new List<List<string>>();

            var queries = titles.Select(GetQueryTitle).Distinct();
            result.Add(queries.Take(1).ToList());
            result.Add(queries.Skip(1).ToList());
            return result;
        }

        public static string GetQueryTitle(string title)
        {
            Ensure.That(title,() => title).IsNotNullOrWhiteSpace();

            var cleanTitle = BeginningThe.Replace(title, string.Empty);

            cleanTitle = cleanTitle.Replace(" & ", " ");
            cleanTitle = SpecialCharacter.Replace(cleanTitle, "");
            cleanTitle = NonWord.Replace(cleanTitle, "+");

            //remove any repeating +s
            cleanTitle = Regex.Replace(cleanTitle, @"\+{2,}", "+");
            cleanTitle = cleanTitle.RemoveAccent();
            cleanTitle = cleanTitle.Trim('+', ' ');

            return cleanTitle.Length == 0 ? title : cleanTitle;
        }
    }
}
