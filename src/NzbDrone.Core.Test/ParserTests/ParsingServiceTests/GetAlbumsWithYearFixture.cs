using System;
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

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.GetAlbumsByArtistMetadataId(It.IsAny<int>()))
                .Returns(new List<Album>());
        }

        private static Album GreatestHits(int id, DateTime? releaseDate, params DateTime[] editionDates)
        {
            return new Album
            {
                Id = id,
                ArtistMetadataId = 1,
                Title = "Greatest Hits",
                CleanTitle = "greatesthits",
                ReleaseDate = releaseDate,
                AlbumReleases = editionDates.Select(d => new AlbumRelease { ReleaseDate = d, Monitored = true }).ToList()
            };
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

        [Test]
        public void should_accept_search_criteria_match_when_year_is_part_of_album_title()
        {
            var album = new Album
            {
                Id = 3,
                Title = "Live 1975",
                CleanTitle = "live1975",
                ReleaseDate = new DateTime(2022, 4, 8)
            };

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { album }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Live 1975",
                ReleaseYear = 1975,
                ReleaseDate = "1975"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(3);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void should_find_album_by_title_with_year_when_parser_split_year_from_title(bool fromSearchCriteria)
        {
            var album = new Album
            {
                Id = 3,
                Title = "Woodstock 1969",
                CleanTitle = "woodstock1969",
                ReleaseDate = new DateTime(2009, 8, 15)
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Woodstock",
                AlbumTitleWithYear = "Woodstock 1969",
                ReleaseYear = 1969,
                ReleaseDate = "1969"
            };

            var criteria = fromSearchCriteria
                ? new AlbumSearchCriteria { Artist = _artist, Albums = new List<Album> { album } }
                : null;

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Woodstock 1969", 1969))
                .Returns(fromSearchCriteria ? null : album);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(3);
            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.FindByTitleAndYearInexact(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void should_prefer_album_titled_with_year_over_album_released_that_year(bool searchForLive)
        {
            var live = new Album
            {
                Id = 3,
                Title = "Live",
                CleanTitle = "live",
                ReleaseDate = new DateTime(1975, 5, 1)
            };

            var liveWithYear = new Album
            {
                Id = 4,
                Title = "Live 1975",
                CleanTitle = "live1975",
                ReleaseDate = new DateTime(2022, 4, 8)
            };

            var criteria = searchForLive
                ? new AlbumSearchCriteria { Artist = _artist, Albums = new List<Album> { live } }
                : null;

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Live",
                AlbumTitleWithYear = "Live 1975",
                ReleaseYear = 1975,
                ReleaseDate = "1975"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Live", 1975))
                .Returns(live);

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Live 1975", 1975))
                .Returns(liveWithYear);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(4);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void should_match_album_released_that_year_when_no_album_is_titled_with_year(bool searchForLive)
        {
            var live = new Album
            {
                Id = 3,
                Title = "Live",
                CleanTitle = "live",
                ReleaseDate = new DateTime(1975, 5, 1)
            };

            var criteria = searchForLive
                ? new AlbumSearchCriteria { Artist = _artist, Albums = new List<Album> { live } }
                : null;

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Live",
                AlbumTitleWithYear = "Live 1975",
                ReleaseYear = 1975,
                ReleaseDate = "1975"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Live", 1975))
                .Returns(live);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(3);
        }

        [Test]
        public void should_not_look_up_title_with_year_when_parser_did_not_find_one()
        {
            var live = new Album
            {
                Id = 3,
                Title = "Live",
                CleanTitle = "live",
                ReleaseDate = new DateTime(1975, 5, 1)
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Live",
                ReleaseYear = 1975,
                ReleaseDate = "1975"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Live", 1975))
                .Returns(live);

            var result = Subject.GetAlbums(parsed, _artist, null);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(3);
            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.FindByTitleAndYear(It.IsAny<int>(), It.IsNotIn("Live"), It.IsAny<int?>()), Times.Never());
        }

        [Test]
        public void should_fall_back_to_title_with_year_when_album_title_alone_is_not_found()
        {
            var album = new Album
            {
                Id = 3,
                Title = "Live (1975)",
                CleanTitle = "live1975",
                ReleaseDate = new DateTime(2022, 4, 8)
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Live",
                ReleaseYear = 1975,
                ReleaseDate = "1975"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Live 1975", 1975))
                .Returns(album);

            var result = Subject.GetAlbums(parsed, _artist, null);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(3);
            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.FindByTitleAndYearInexact(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never());
        }

        [Test]
        public void should_load_releases_and_accept_search_criteria_album_when_year_matches_edition()
        {
            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { _albums[0] }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2024,
                ReleaseDate = "2024"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.LoadReleases(It.IsAny<List<Album>>()))
                .Callback<List<Album>>(albums => albums.ForEach(a => a.AlbumReleases = new List<AlbumRelease> { new AlbumRelease { ReleaseDate = new DateTime(2024, 2, 1) } }));

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void should_prefer_original_year_over_edition_year_when_same_title_albums_tie(bool reverseOrder)
        {
            var withEdition = new Album
            {
                Id = 1,
                Title = "Greatest Hits",
                CleanTitle = "greatesthits",
                ReleaseDate = new DateTime(2010, 6, 15),
                AlbumReleases = new List<AlbumRelease> { new AlbumRelease { ReleaseDate = new DateTime(2020, 1, 1) } }
            };

            var original = new Album
            {
                Id = 2,
                Title = "Greatest Hits",
                CleanTitle = "greatesthits",
                ReleaseDate = new DateTime(2020, 3, 20),
                AlbumReleases = new List<AlbumRelease>()
            };

            var albums = reverseOrder ? new List<Album> { original, withEdition } : new List<Album> { withEdition, original };

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = albums
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns((Album)null);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(2);
        }

        [Test]
        public void should_not_pick_same_title_album_when_years_tie_exactly()
        {
            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album>
                {
                    new Album { Id = 1, Title = "Greatest Hits", CleanTitle = "greatesthits", ReleaseDate = new DateTime(2019, 6, 15), AlbumReleases = new List<AlbumRelease>() },
                    new Album { Id = 2, Title = "Greatest Hits", CleanTitle = "greatesthits", ReleaseDate = new DateTime(2021, 6, 15), AlbumReleases = new List<AlbumRelease>() }
                }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns((Album)null);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().BeEmpty();
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void should_not_map_to_searched_album_matching_only_by_edition_when_other_album_was_released_that_year(bool otherAlbumExists, bool expectSearchedAlbum)
        {
            var searched = GreatestHits(1, new DateTime(2010, 6, 15), new DateTime(2020, 1, 1));

            var other = GreatestHits(2, new DateTime(2020, 3, 20));

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.GetAlbumsByArtistMetadataId(1))
                .Returns(otherAlbumExists ? new List<Album> { searched, other } : new List<Album> { searched });

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { searched }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            if (expectSearchedAlbum)
            {
                result.Should().HaveCount(1);
                result[0].Id.Should().Be(1);
            }
            else
            {
                result.Should().NotContain(a => a.Id == 1);
            }

            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.GetAlbumsByArtistMetadataId(1), Times.Once());
        }

        [Test]
        public void should_not_treat_albums_without_clean_title_as_same_title()
        {
            var searched = GreatestHits(1, new DateTime(2010, 6, 15), new DateTime(2020, 1, 1));
            var other = GreatestHits(2, new DateTime(2020, 3, 20));
            searched.CleanTitle = null;
            other.CleanTitle = null;

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.GetAlbumsByArtistMetadataId(1))
                .Returns(new List<Album> { searched, other });

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { searched }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
        }

        [Test]
        public void should_keep_searched_album_when_other_album_matches_year_worse_than_edition()
        {
            var searched = GreatestHits(1, new DateTime(2010, 6, 15), new DateTime(2024, 1, 1));

            var other = GreatestHits(2, new DateTime(2020, 3, 20));

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.GetAlbumsByArtistMetadataId(1))
                .Returns(new List<Album> { searched, other });

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { searched }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2024,
                ReleaseDate = "2024"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
        }

        [Test]
        public void should_not_look_up_other_albums_when_searched_album_matches_by_original_year()
        {
            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { _albums[0] }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2010,
                ReleaseDate = "2010"
            };

            Subject.GetAlbums(parsed, _artist, criteria);

            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.GetAlbumsByArtistMetadataId(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_not_map_to_best_searched_album_matching_only_by_edition_when_other_album_was_released_that_year()
        {
            var monitoredWithEdition = GreatestHits(1, new DateTime(1990, 6, 15), new DateTime(2020, 1, 1));

            var monitoredOld = GreatestHits(2, new DateTime(1980, 6, 15));

            var unmonitored = GreatestHits(3, new DateTime(2020, 3, 20));

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.GetAlbumsByArtistMetadataId(1))
                .Returns(new List<Album> { monitoredWithEdition, monitoredOld, unmonitored });

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { monitoredWithEdition, monitoredOld }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().NotContain(a => a.Id == 1 || a.Id == 2);
        }

        [Test]
        public void should_not_map_to_searched_album_when_other_album_has_closer_edition_year()
        {
            var searched = GreatestHits(1, new DateTime(1973, 6, 15), new DateTime(2018, 1, 1));

            var other = GreatestHits(2, new DateTime(1990, 3, 20), new DateTime(2020, 1, 1));

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.GetAlbumsByArtistMetadataId(1))
                .Returns(new List<Album> { searched, other });

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { searched }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                ReleaseYear = 2020,
                ReleaseDate = "2020"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().NotContain(a => a.Id == 1);
            Mocker.GetMock<IAlbumService>()
                .Verify(s => s.LoadReleases(It.Is<List<Album>>(l => l.Count == 1 && l[0].Id == 2)), Times.Once());
        }

        [Test]
        public void should_keep_searched_album_matching_by_year_in_title_when_other_album_has_same_title()
        {
            var searched = new Album
            {
                Id = 1,
                ArtistMetadataId = 1,
                Title = "Live 1975",
                CleanTitle = "live1975",
                ReleaseDate = new DateTime(2022, 4, 8),
                AlbumReleases = new List<AlbumRelease>()
            };

            var other = new Album
            {
                Id = 2,
                ArtistMetadataId = 1,
                Title = "Live 1975",
                CleanTitle = "live1975",
                ReleaseDate = new DateTime(2010, 4, 8),
                AlbumReleases = new List<AlbumRelease>()
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.GetAlbumsByArtistMetadataId(1))
                .Returns(new List<Album> { searched, other });

            var criteria = new AlbumSearchCriteria
            {
                Artist = _artist,
                Albums = new List<Album> { searched }
            };

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Live",
                AlbumTitleWithYear = "Live 1975",
                ReleaseYear = 1975,
                ReleaseDate = "1975"
            };

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
        }

        [Test]
        public void should_still_use_unreliable_year_to_pick_same_title_album()
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
                YearConfidence = YearMatchConfidence.Medium,
                ReleaseDate = "2020"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", 2020))
                .Returns(_albums[1]);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(2);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void should_prefer_album_titled_with_qualifier_over_truncated_title(bool searchForOriginal)
        {
            var original = new Album { Id = 3, Title = "1989", CleanTitle = "1989", ReleaseDate = new DateTime(2014, 10, 24) };
            var taylorsVersion = new Album { Id = 4, Title = "1989 (Taylor’s Version)", CleanTitle = "1989taylorsversion", ReleaseDate = new DateTime(2023, 10, 27) };

            var criteria = searchForOriginal
                ? new AlbumSearchCriteria { Artist = _artist, Albums = new List<Album> { original } }
                : null;

            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "1989",
                AlbumTitleWithQualifier = "1989 (Taylor's Version)",
                ReleaseYear = 2023,
                ReleaseDate = "2023"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "1989 (Taylor's Version)", 2023))
                .Returns(taylorsVersion);

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "1989", 2023))
                .Returns(original);

            var result = Subject.GetAlbums(parsed, _artist, criteria);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(4);
        }

        [Test]
        public void should_fall_back_to_album_title_when_qualified_title_is_not_found()
        {
            var parsed = new ParsedAlbumInfo
            {
                AlbumTitle = "Greatest Hits",
                AlbumTitleWithQualifier = "Greatest Hits (Deluxe Edition)",
                ReleaseYear = 2010,
                ReleaseDate = "2010"
            };

            Mocker.GetMock<IAlbumService>()
                .Setup(s => s.FindByTitleAndYear(_artist.ArtistMetadataId, "Greatest Hits", 2010))
                .Returns(_albums[0]);

            var result = Subject.GetAlbums(parsed, _artist, null);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
        }
    }
}
