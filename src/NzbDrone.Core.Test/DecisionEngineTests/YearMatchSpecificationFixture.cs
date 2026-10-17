using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class YearMatchSpecificationFixture : CoreTest<YearMatchSpecification>
    {
        private Artist _artist;
        private Album _album;
        private RemoteAlbum _remoteAlbum;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>.CreateNew().With(s => s.Id = 1).Build();
            _album = Builder<Album>.CreateNew()
                .With(s => s.ReleaseDate = new DateTime(2020, 6, 15))
                .Build();

            _remoteAlbum = new RemoteAlbum
            {
                Artist = _artist,
                Albums = new List<Album> { _album },
                ParsedAlbumInfo = new ParsedAlbumInfo
                {
                    AlbumTitle = "Test Album",
                    ReleaseYear = 2020
                },
                Release = new ReleaseInfo
                {
                    Title = "Artist - Test Album (2020) FLAC"
                }
            };

            Mocker.SetConstant<IAlbumYearMatcher>(new AlbumYearMatcher());
        }

        [Test]
        public void should_accept_when_no_year_in_parsed_info()
        {
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = null;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_no_albums()
        {
            _remoteAlbum.Albums = new List<Album>();

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_year_matches_exactly()
        {
            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_year_is_within_tolerance()
        {
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = 2021;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_year_difference_is_within_fuzzy_range()
        {
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = 2023;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_year_difference_exceeds_hard_limit()
        {
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = 2010;

            var result = Subject.IsSatisfiedBy(_remoteAlbum, null);

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Contain("does not match");
        }

        [Test]
        public void should_accept_when_album_has_no_release_date()
        {
            _album.ReleaseDate = null;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_check_all_albums_in_release()
        {
            var album2 = Builder<Album>.CreateNew()
                .With(s => s.ReleaseDate = new DateTime(2010, 1, 1))
                .Build();

            _remoteAlbum.Albums = new List<Album> { _album, album2 };
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = 2020;

            var result = Subject.IsSatisfiedBy(_remoteAlbum, null);

            result.Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_multi_album_when_all_years_match()
        {
            var album2 = Builder<Album>.CreateNew()
                .With(s => s.ReleaseDate = new DateTime(2020, 8, 20))
                .Build();

            _remoteAlbum.Albums = new List<Album> { _album, album2 };

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [TestCase(2020, 2020, true)]
        [TestCase(2020, 2021, true)]
        [TestCase(2020, 2019, true)]
        [TestCase(2020, 2023, true)]
        [TestCase(2020, 2025, true)]
        [TestCase(2020, 2026, false)]
        [TestCase(2020, 2014, false)]
        [TestCase(2020, 2005, false)]
        public void should_handle_various_year_differences(int albumYear, int parsedYear, bool expectedAccepted)
        {
            _album.ReleaseDate = new DateTime(albumYear, 6, 15);
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = parsedYear;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(expectedAccepted);
        }

        [TestCase("Woodstock 1969", 2009, 1969)]
        [TestCase("1989", 2014, 1989)]
        [TestCase("Live 1975", 2022, 1975)]
        public void should_accept_when_parsed_year_is_part_of_album_title(string albumTitle, int albumYear, int parsedYear)
        {
            _album.Title = albumTitle;
            _album.ReleaseDate = new DateTime(albumYear, 6, 15);
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = parsedYear;
            _remoteAlbum.Release.Title = $"Artist - {albumTitle} FLAC";

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_other_year_of_numbered_series()
        {
            _album.Title = "Now That's What I Call Music! 2023";
            _album.ReleaseDate = new DateTime(2023, 11, 17);
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = 2010;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_label_year_difference_in_rejection_reason()
        {
            _album.Title = "Test Album";
            _album.ReleaseDate = new DateTime(1993, 6, 15);
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = 2024;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Reason.Should().Be("Release year 2024 does not match album year 1993 (31 years apart)");
        }

        [Test]
        public void should_load_releases_and_accept_edition_year()
        {
            _album.ReleaseDate = new DateTime(1973, 3, 1);
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = 2011;

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.LoadReleases(It.IsAny<List<Album>>()))
                .Callback<List<Album>>(albums => albums.ForEach(a => a.AlbumReleases = new List<AlbumRelease> { new AlbumRelease { ReleaseDate = new DateTime(2011, 9, 26) } }));

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_not_load_releases_when_no_year_parsed()
        {
            _remoteAlbum.ParsedAlbumInfo.ReleaseYear = null;

            Subject.IsSatisfiedBy(_remoteAlbum, null);

            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.LoadReleases(It.IsAny<List<Album>>()), Times.Never());
        }
    }
}
