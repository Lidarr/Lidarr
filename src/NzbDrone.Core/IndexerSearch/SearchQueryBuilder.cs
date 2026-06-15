using System.Text.RegularExpressions;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.IndexerSearch
{
    public interface IBuildSearchQuery
    {
        string BuildAlbumSearchQuery(AlbumSearchCriteria criteria);
        string BuildArtistSearchQuery(ArtistSearchCriteria criteria);
        bool UseCustomFormat { get; }
    }

    public class SearchQueryBuilder : IBuildSearchQuery
    {
        private readonly ISearchFormatConfigService _configService;
        private static readonly Regex TokenRegex = new Regex(@"\{(?<token>[A-Za-z ]+)\}", RegexOptions.Compiled);

        public SearchFormatConfig CustomConfig { get; set; }

        public SearchQueryBuilder(ISearchFormatConfigService configService)
        {
            _configService = configService;
        }

        private SearchFormatConfig Config => CustomConfig ?? _configService.GetConfig();

        public bool UseCustomFormat => Config.UseCustomSearchFormat;

        public string BuildAlbumSearchQuery(AlbumSearchCriteria criteria)
        {
            var config = Config;
            if (!config.UseCustomSearchFormat)
            {
                return null;
            }

            return ResolveFormat(config.AlbumSearchFormat, criteria);
        }

        public string BuildArtistSearchQuery(ArtistSearchCriteria criteria)
        {
            var config = Config;
            if (!config.UseCustomSearchFormat)
            {
                return null;
            }

            return ResolveFormat(config.ArtistSearchFormat, criteria);
        }

        private string ResolveFormat(string format, SearchCriteriaBase criteria)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return string.Empty;
            }

            var albumCriteria = criteria as AlbumSearchCriteria;

            var result = TokenRegex.Replace(format, match =>
            {
                var token = match.Groups["token"].Value;

                switch (token.ToLowerInvariant())
                {
                    case "artist name":
                        return criteria.ArtistQuery ?? string.Empty;
                    case "artist cleanname":
                        return criteria.CleanArtistQuery ?? string.Empty;
                    case "album title":
                        return albumCriteria?.AlbumTitle ?? string.Empty;
                    case "album cleantitle":
                        return albumCriteria?.CleanAlbumQuery ?? string.Empty;
                    case "album year":
                        return albumCriteria != null && albumCriteria.AlbumYear > 0 ? albumCriteria.AlbumYear.ToString() : string.Empty;
                    case "album disambiguation":
                        return albumCriteria?.Disambiguation ?? string.Empty;
                    default:
                        return match.Value;
                }
            });

            // Clean up double spaces that might result from empty token replacements
            result = Regex.Replace(result, @"\s+", " ").Trim();
            return result;
        }
    }
}
