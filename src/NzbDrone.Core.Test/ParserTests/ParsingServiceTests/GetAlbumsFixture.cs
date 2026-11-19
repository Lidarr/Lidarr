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

        [TestCase("These Days", true)]
        [TestCase("these.days", true)]
        [TestCase("An unrelated song", false)]
        [TestCase("These Days Live", false)]
        public void should_map_only_the_requested_song_title_to_its_parent_album(string title, bool matches)
        {
            var artist = new Artist { Id = 1 };
            var album = new Album { Id = 10, Title = "TruANT" };
            var criteria = new TrackSearchCriteria
            {
                Artist = artist,
                Albums = new List<Album> { album },
                Tracks = new List<Track> { new Track { Title = "These Days", ForeignRecordingId = "wanted" } }
            };

            var result = Subject.GetAlbums(new ParsedAlbumInfo { AlbumTitle = title }, artist, criteria);

            if (matches)
            {
                result.Should().ContainSingle().Which.Should().Be(album);
            }
            else
            {
                result.Should().BeEmpty();
            }
        }

        [TestCase("Alien Ant Farm - These Days [FLAC]", true)]
        [TestCase("Alien Ant Farm - These Days Live [FLAC]", false)]
        [TestCase("Another Artist - These Days [FLAC]", false)]
        public void should_target_a_parsed_song_release_without_changing_its_parent_album_title(string title, bool matches)
        {
            var artist = new Artist { Id = 1, Name = "Alien Ant Farm", CleanName = "alienantfarm" };
            var album = new Album { Id = 10, Title = "TruANT" };
            var criteria = new TrackSearchCriteria
            {
                Artist = artist,
                Albums = new List<Album> { album },
                Tracks = new List<Track> { new Track { Title = "These Days", ForeignRecordingId = "wanted" } }
            };

            var result = Subject.Map(Parser.Parser.ParseAlbumTitle(title), criteria);

            if (matches)
            {
                result.TrackSearchPriority.Should().Be(0);
                result.TargetRecordingIds.Should().Equal("wanted");
                result.Albums.Should().ContainSingle().Which.Title.Should().Be("TruANT");
            }
            else
            {
                result.TrackSearchPriority.Should().BeNull();
            }
        }
    }
}
