using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.SearchFormatTests
{
    [TestFixture]
    public class SearchQueryBuilderFixture : CoreTest<SearchQueryBuilder>
    {
        private SearchFormatConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new SearchFormatConfig
            {
                UseCustomSearchFormat = true,
                AlbumSearchFormat = "{Artist Name} {Album Title}",
                ArtistSearchFormat = "{Artist Name}"
            };

            Mocker.GetMock<ISearchFormatConfigService>()
                .Setup(v => v.GetConfig())
                .Returns(() => _config);
        }

        [Test]
        public void should_return_null_if_custom_format_disabled()
        {
            _config.UseCustomSearchFormat = false;

            var criteria = new AlbumSearchCriteria
            {
                Artist = new Artist { Name = "Artist" },
                AlbumTitle = "Album"
            };

            var result = Subject.BuildAlbumSearchQuery(criteria);
            result.Should().BeNull();
        }

        [Test]
        public void should_resolve_artist_and_album_tokens()
        {
            var criteria = new AlbumSearchCriteria
            {
                Artist = new Artist { Name = "Linkin Park" },
                AlbumTitle = "Meteora",
                AlbumYear = 2003,
                Disambiguation = "Special Edition"
            };

            _config.AlbumSearchFormat = "{Artist Name} - {Album Title} ({Album Year}) [{Album Disambiguation}]";

            var result = Subject.BuildAlbumSearchQuery(criteria);
            result.Should().Be("Linkin Park - Meteora (2003) [Special Edition]");
        }

        [Test]
        public void should_resolve_clean_names()
        {
            var criteria = new AlbumSearchCriteria
            {
                Artist = new Artist { Name = "The Rolling Stones" },
                AlbumTitle = "Let It Bleed"
            };

            _config.AlbumSearchFormat = "{Artist CleanName} {Album CleanTitle}";

            var result = Subject.BuildAlbumSearchQuery(criteria);
            result.Should().Be("Rolling+Stones Let+It+Bleed");
        }

        [Test]
        public void should_resolve_artist_only_format()
        {
            var criteria = new ArtistSearchCriteria
            {
                Artist = new Artist { Name = "Radiohead" }
            };

            _config.ArtistSearchFormat = "{Artist Name} {Artist CleanName}";

            var result = Subject.BuildArtistSearchQuery(criteria);
            result.Should().Be("Radiohead Radiohead");
        }
    }
}
