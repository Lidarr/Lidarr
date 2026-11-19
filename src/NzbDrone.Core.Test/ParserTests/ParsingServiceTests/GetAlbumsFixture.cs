using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class GetAlbumsFixture : CoreTest<ParsingService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IAlbumYearMatcher>(new AlbumYearMatcher());
        }

        [Test]
        public void should_not_fail_if_search_criteria_contains_multiple_albums_with_the_same_name()
        {
            var artist = Builder<Artist>.CreateNew().Build();
            var albums = Builder<Album>.CreateListOfSize(2).All().With(x => x.Title = "IdenticalTitle").Build().ToList();
            var criteria = new AlbumSearchCriteria
            {
                Artist = artist,
                Albums = albums
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "IdenticalTitle",
                ReleaseYear = null
            };

            Subject.GetAlbums(parsed, artist, criteria).Should().BeEquivalentTo(new List<Album>());

            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.FindByTitleAndYear(artist.ArtistMetadataId, "IdenticalTitle", null), Times.Once());
        }

        [Test]
        public void should_return_empty_when_album_title_is_null()
        {
            var artist = Builder<Artist>.CreateNew().Build();
            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = null
            };

            var result = Subject.GetAlbums(parsed, artist, null);

            result.Should().BeEmpty();
        }

        [Test]
        public void should_use_year_for_disambiguation()
        {
            var artist = Builder<Artist>.CreateNew().Build();
            var album = Builder<Album>.CreateNew()
                .With(x => x.Title = "TestAlbum")
                .Build();

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "TestAlbum",
                ReleaseYear = 2020
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(artist.ArtistMetadataId, "TestAlbum", 2020))
                .Returns(album);

            var result = Subject.GetAlbums(parsed, artist, null);

            result.Should().HaveCount(1);
            result[0].Should().Be(album);
        }
    }
}
