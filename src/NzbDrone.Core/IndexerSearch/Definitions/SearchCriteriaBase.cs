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
        private static readonly Regex NonWord = new Regex(@"[^\w']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BeginningThe = new Regex(@"^the\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StandardizeSingleQuotesRegex = new Regex(@"[\u0060\u00B4\u2018\u2019]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public virtual bool MonitoredEpisodesOnly { get; set; }
        public virtual bool UserInvokedSearch { get; set; }
        public virtual bool InteractiveSearch { get; set; }

        public Artist Artist { get; set; }
        public List<Album> Albums { get; set; }
        public List<Track> Tracks { get; set; }
        public List<string> ArtistTitles { get; set; }

        public string ArtistQuery => Artist.SearchName;
        public string CleanArtistQuery => GetQueryTitle(ArtistQuery);
        public List<string> CleanArtistTitles => ArtistTitles?.Select(GetQueryTitle).Distinct().ToList() ?? new List<string> { CleanArtistQuery };

        public static string GetQueryTitle(string title)
        {
            Ensure.That(title, () => title).IsNotNullOrWhiteSpace();

            // Most VA albums are listed as VA, not Various Artists
            if (title == "Various Artists")
            {
                title = "VA";
            }

            var cleanTitle = BeginningThe.Replace(title, string.Empty);
            cleanTitle = StandardizeSingleQuotesRegex.Replace(cleanTitle, "'");
            cleanTitle = NonWord.Replace(cleanTitle, "+");

            // remove any repeating +s
            cleanTitle = Regex.Replace(cleanTitle, @"\+{2,}", "+");
            cleanTitle = cleanTitle.RemoveAccent();
            cleanTitle = cleanTitle.Trim('+', ' ');

            return cleanTitle.Length == 0 ? title : cleanTitle;
        }
    }
}
