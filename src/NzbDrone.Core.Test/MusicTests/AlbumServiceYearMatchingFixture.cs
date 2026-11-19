using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.AlbumRepositoryTests
{
    [TestFixture]
    public class AlbumServiceYearMatchingFixture : CoreTest<AlbumService>
    {
        private List<Album> _albums;

        [SetUp]
        public void Setup()
        {
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
                },
                new Album
                {
                    Id = 3,
                    Title = "Greatest Hits Vol. 2",
                    CleanTitle = "greatesthitsvol2",
                    ReleaseDate = new DateTime(2015, 8, 10)
                },
                new Album
                {
                    Id = 4,
                    Title = "Peppermint Winter",
                    CleanTitle = "peppermintwinter",
                    ReleaseDate = new DateTime(2012, 12, 1)
                },
                new Album
                {
                    Id = 5,
                    Title = "Peppermint Winter (Remastered)",
                    CleanTitle = "peppermintwinterremastered",
                    ReleaseDate = new DateTime(2022, 12, 1)
                },
                new Album
                {
                    Id = 6,
                    Title = "Album Without Date",
                    CleanTitle = "albumwithoutdate",
                    ReleaseDate = null
                }
            };

            Mocker.GetMock<IAlbumRepository>()
                .Setup(s => s.GetAlbumsByArtistMetadataId(It.IsAny<int>()))
                .Returns(_albums);

            Mocker.GetMock<IAlbumRepository>()
                .Setup(s => s.FindByTitle(It.IsAny<int>(), "Peppermint Winter"))
                .Returns(_albums[3]);

            Mocker.GetMock<IAlbumRepository>()
                .Setup(s => s.FindByTitle(It.IsAny<int>(), "Greatest Hits"))
                .Returns(_albums[0]);

            Mocker.SetConstant<IAlbumYearMatcher>(new AlbumYearMatcher());
        }

        [Test]
        public void should_return_album_when_year_matches_exactly()
        {
            var album = Subject.FindByTitleAndYear(0, "Peppermint Winter", 2012);

            album.Should().NotBeNull();
            album.Id.Should().Be(4);
            album.ReleaseDate.Value.Year.Should().Be(2012);
        }

        [Test]
        public void should_return_album_when_year_is_within_acceptable_range()
        {
            var album = Subject.FindByTitleAndYear(0, "Peppermint Winter", 2013);

            album.Should().NotBeNull();
            album.Id.Should().Be(4);
        }

        [Test]
        public void should_return_null_when_year_differs_significantly()
        {
            var album = Subject.FindByTitleAndYear(0, "Peppermint Winter", 2020);

            album.Should().BeNull();
        }

        [Test]
        public void should_return_album_when_year_is_null()
        {
            var album = Subject.FindByTitleAndYear(0, "Peppermint Winter", null);

            album.Should().NotBeNull();
            album.Id.Should().Be(4);
        }

        [Test]
        public void should_return_null_when_album_not_found()
        {
            Mocker.GetMock<IAlbumRepository>()
                .Setup(s => s.FindByTitle(It.IsAny<int>(), "Nonexistent Album"))
                .Returns((Album)null);

            var album = Subject.FindByTitleAndYear(0, "Nonexistent Album", 2012);

            album.Should().BeNull();
        }

        [Test]
        public void should_find_album_by_inexact_title_and_matching_year()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Peppermint Wintr", 2012);

            album.Should().NotBeNull();
            album.Id.Should().Be(4);
        }

        [Test]
        public void should_prefer_album_with_matching_year_when_similar_titles_exist()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Greatest Hits", 2010);

            album.Should().NotBeNull();
            album.Id.Should().Be(1);
            album.ReleaseDate.Value.Year.Should().Be(2010);
        }

        [Test]
        public void should_prefer_album_with_matching_year_2020()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Greatest Hits", 2020);

            album.Should().NotBeNull();
            album.Id.Should().Be(2);
            album.ReleaseDate.Value.Year.Should().Be(2020);
        }

        [Test]
        public void should_distinguish_original_from_remastered_by_year()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Peppermint Winter", 2012);

            album.Should().NotBeNull();
            album.Id.Should().Be(4);
            album.Title.Should().Be("Peppermint Winter");
        }

        [Test]
        public void should_find_remastered_version_by_year()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Peppermint Winter", 2022);

            album.Should().NotBeNull();
            album.Id.Should().Be(5);
            album.Title.Should().Be("Peppermint Winter (Remastered)");
        }

        [Test]
        public void should_fall_back_to_title_only_matching_when_year_is_null()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Peppermint Wintr", null);

            album.Should().NotBeNull();
        }

        [Test]
        public void should_return_null_when_no_matching_albums()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Completely Unknown Album Title XYZ", 2012);

            album.Should().BeNull();
        }

        [Test]
        public void should_handle_album_without_release_date()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Album Without Date", 2015);

            album.Should().NotBeNull();
            album.Id.Should().Be(6);
        }

        [Test]
        public void should_return_candidates_filtered_by_year()
        {
            var candidates = Subject.GetCandidates(0, "Greatest Hits", 2010);

            candidates.Should().NotBeEmpty();
            candidates[0].ReleaseDate.Value.Year.Should().Be(2010);
        }

        [Test]
        public void should_return_candidates_without_year_filter()
        {
            var candidates = Subject.GetCandidates(0, "Greatest Hits", null);

            candidates.Should().NotBeEmpty();
        }

        [Test]
        public void should_give_small_bonus_for_one_year_difference()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Greatest Hits", 2011);

            album.Should().NotBeNull();
            album.Id.Should().Be(1);
        }

        [Test]
        public void should_penalize_large_year_differences()
        {
            var album = Subject.FindByTitleAndYearInexact(0, "Greatest Hits Vol. 2", 2015);

            album.Should().NotBeNull();
            album.Id.Should().Be(3);
        }
    }
}
