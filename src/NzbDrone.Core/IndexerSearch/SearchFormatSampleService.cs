using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchFormatSampleService
    {
        SearchFormatExampleResult GetExamples(string albumFormat, string artistFormat);
    }

    public class SearchFormatExampleResult
    {
        public string AlbumSearchExample { get; set; }
        public string ArtistSearchExample { get; set; }
    }

    public class SearchFormatSampleService : ISearchFormatSampleService
    {
        public SearchFormatExampleResult GetExamples(string albumFormat, string artistFormat)
        {
            var artist = new Artist
            {
                Name = "The Artist Name"
            };

            var albumCriteria = new AlbumSearchCriteria
            {
                Artist = artist,
                AlbumTitle = "The Album Title",
                AlbumYear = 2026,
                Disambiguation = "Deluxe Edition"
            };

            var artistCriteria = new ArtistSearchCriteria
            {
                Artist = artist
            };

            var tempConfig = new SearchFormatConfig
            {
                UseCustomSearchFormat = true,
                AlbumSearchFormat = albumFormat,
                ArtistSearchFormat = artistFormat
            };

            var queryBuilder = new SearchQueryBuilder(null as ISearchFormatConfigService) { CustomConfig = tempConfig };

            return new SearchFormatExampleResult
            {
                AlbumSearchExample = queryBuilder.BuildAlbumSearchQuery(albumCriteria),
                ArtistSearchExample = queryBuilder.BuildArtistSearchQuery(artistCriteria)
            };
        }
    }
}
