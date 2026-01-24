using System;
using System.Collections.Generic;
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
    public class GetAlbumsWithYearFixture : CoreTest<ParsingService>
    {
        private Artist _artist;
        private List<Album> _albums;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>.CreateNew()
                .With(a => a.ArtistMetadataId = 1)
                .Build();

            _albums = new List<Album>
            {
                new Album
                {
                    Id = 1,
                    Title = "Greatest Hits",
                    CleanTitle = "greatesthits",
                    ReleaseDate = new DateTime(2010, 6, 15)
                },
                new Album
                {
                    Id = 2,
                    Title = "Greatest Hits",
                    CleanTitle = "greatesthits",
                    ReleaseDate = new DateTime(2020, 3, 20)
                }
            };

            Mocker.SetConstant<IAlbumYearMatcher>(new AlbumYearMatcher());
        }

        [Test]
        public void should_use_year_from_parsed_album_info_for_matching()
        {
            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2010,
                ReleaseDate = "2010"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", 2010))
                .Returns(_albums[0]);

            var result = Subject.GetAlbums(parsed, _artist, null);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
            result[0].ReleaseDate.Value.Year.Should().Be(2010);
        }

        [Test]
        public void should_match_2020_album_when_year_is_2020()
        {
            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", 2020))
                .Returns(_albums[1]);

            var result = Subject.GetAlbums(parsed, _artist, null);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(2);
            result[0].ReleaseDate.Value.Year.Should().Be(2020);
        }

        [Test]
        public void should_fall_back_to_inexact_matching_when_exact_match_not_found()
        {
            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2010,
                ReleaseDate = "2010"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", 2010))
                .Returns((Album)null);

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYearInexact(_artist.ArtistMetadataId, "Greatest Hits", 2010))
                .Returns(_albums[0]);

            var result = Subject.GetAlbums(parsed, _artist, null);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
        }

        [Test]
        public void should_work_without_year_in_parsed_info()
        {
            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = null,
                ReleaseDate = null
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", null))
                .Returns(_albums[0]);

            var result = Subject.GetAlbums(parsed, _artist, null);

            result.Should().HaveCount(1);
        }

        [Test]
        public void should_use_release_year_property_directly()
        {
            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2010,
                ReleaseDate = "2010"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", 2010))
                .Returns(_albums[0]);

            Subject.GetAlbums(parsed, _artist, null);

            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", 2010), Times.Once());
        }

        [Test]
        public void should_verify_year_when_using_search_criteria_with_matching_title()
        {
            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = _albums
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(2);
        }

        [Test]
        public void should_reject_search_criteria_match_when_year_differs_significantly()
        {
            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { _albums[0] }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", 2020))
                .Returns(_albums[1]);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(2);
        }

        [Test]
        public void should_accept_search_criteria_match_when_year_is_close()
        {
            var album2011 = new Album
            {
                Id = 3,
                Title = "Greatest Hits",
                CleanTitle = "greatesthits",
                ReleaseDate = new DateTime(2011, 1, 1)
            };

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { album2011 }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2010,
                ReleaseDate = "2010"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(3);
        }

        [Test]
        public void should_handle_null_release_year_with_search_criteria()
        {
            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = _albums
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = null,
                ReleaseDate = null
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", null))
                .Returns(_albums[0]);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
        }
    }
}
