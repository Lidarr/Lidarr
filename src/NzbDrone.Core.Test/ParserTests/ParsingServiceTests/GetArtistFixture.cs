using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class GetArtistFixture : CoreTest<ParsingService>
    {
        private const string AmbiguousTitle = "Some Artist - Some Album (2019) [FLAC]";

        private Artist _artist;
        private Artist _otherArtist;

        [SetUp]
        public void Setup()
        {
            _artist = new Artist { Id = 1, ArtistMetadataId = 10, Name = "Some Artist" };
            _otherArtist = new Artist { Id = 2, ArtistMetadataId = 20, Name = "Some-Artist" };
        }

        private void GivenArtistsWithSameCleanName()
        {
            Mocker.GetMock<IArtistService>()
                  .Setup(s => s.FindByName(It.IsAny<string>()))
                  .Throws(new MultipleArtistsFoundException(new List<Artist> { _artist, _otherArtist }, "Expected one artist, but found 2"));
        }

        private void GivenAlbumForArtist(Artist artist)
        {
            Mocker.GetMock<IAlbumService>()
                  .Setup(s => s.FindByTitleAndYear(artist.ArtistMetadataId, It.IsAny<string>(), It.IsAny<int?>()))
                  .Returns(new Album { ArtistMetadataId = artist.ArtistMetadataId });
        }

        [Test]
        public void should_use_passed_in_title_when_it_cannot_be_parsed()
        {
            const string title = "30 Rock";

            Subject.GetArtist(title);

            Mocker.GetMock<IArtistService>()
                  .Verify(s => s.FindByName(title), Times.Once());
        }

        [Test]
        public void should_use_parsed_artist_title()
        {
            const string title = "30 Rock - Get Some [FLAC]";

            Subject.GetArtist(title);

            Mocker.GetMock<IArtistService>()
                  .Verify(s => s.FindByName(Parser.Parser.ParseAlbumTitle(title).ArtistName), Times.Once());
        }

        [Test]
        public void should_use_album_to_choose_between_artists_with_same_clean_name()
        {
            GivenArtistsWithSameCleanName();
            GivenAlbumForArtist(_artist);

            Subject.GetArtist(AmbiguousTitle).Should().Be(_artist);
        }

        [Test]
        public void should_throw_if_no_artist_with_same_clean_name_has_album()
        {
            GivenArtistsWithSameCleanName();

            Assert.Throws<MultipleArtistsFoundException>(() => Subject.GetArtist(AmbiguousTitle));
        }

        [Test]
        public void should_throw_if_multiple_artists_with_same_clean_name_have_album()
        {
            GivenArtistsWithSameCleanName();
            GivenAlbumForArtist(_artist);
            GivenAlbumForArtist(_otherArtist);

            Assert.Throws<MultipleArtistsFoundException>(() => Subject.GetArtist(AmbiguousTitle));
        }

        [Test]
        public void should_throw_if_there_is_no_album_title_to_disambiguate()
        {
            GivenArtistsWithSameCleanName();

            Assert.Throws<MultipleArtistsFoundException>(() => Subject.GetArtist("Some Artist"));
        }

        [Test]
        public void should_use_release_year_to_match_album()
        {
            GivenArtistsWithSameCleanName();
            GivenAlbumForArtist(_artist);

            var parsedAlbumInfo = Parser.Parser.ParseAlbumTitle(AmbiguousTitle);
            parsedAlbumInfo.ReleaseYear.Should().Be(2019);

            Subject.GetArtist(AmbiguousTitle);

            Mocker.GetMock<IAlbumService>()
                  .Verify(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, parsedAlbumInfo.AlbumTitle, 2019), Times.Once());
        }

        [Test]
        public void should_map_to_artist_with_album_if_multiple_artists_have_same_clean_name()
        {
            GivenArtistsWithSameCleanName();
            GivenAlbumForArtist(_artist);

            Subject.Map(Parser.Parser.ParseAlbumTitle(AmbiguousTitle)).Artist.Should().Be(_artist);
        }
    }
}
