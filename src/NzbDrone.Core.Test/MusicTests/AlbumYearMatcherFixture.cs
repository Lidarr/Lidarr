using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class AlbumYearMatcherFixture : CoreTest<AlbumYearMatcher>
    {
        [Test]
        public void should_return_match_when_parsed_year_is_null()
        {
            var result = Subject.Match(new DateTime(2020, 1, 1), null);

            result.IsMatch.Should().BeTrue();
            result.YearDifference.Should().BeNull();
            result.ScoreAdjustment.Should().Be(0);
        }

        [Test]
        public void should_return_match_when_album_date_is_null()
        {
            var result = Subject.Match((DateTime?)null, 2020);

            result.IsMatch.Should().BeTrue();
            result.YearDifference.Should().Be(0);
            result.Confidence.Should().Be(YearMatchConfidence.Low);
        }

        [Test]
        public void should_give_high_bonus_for_exact_year_match()
        {
            var result = Subject.Match(new DateTime(2020, 6, 15), 2020);

            result.IsMatch.Should().BeTrue();
            result.YearDifference.Should().Be(0);
            result.ScoreAdjustment.Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
            result.Confidence.Should().Be(YearMatchConfidence.High);
        }

        [Test]
        public void should_give_medium_bonus_for_one_year_difference()
        {
            var result = Subject.Match(new DateTime(2020, 6, 15), 2021);

            result.IsMatch.Should().BeTrue();
            result.YearDifference.Should().Be(1);
            result.ScoreAdjustment.Should().Be(AlbumYearMatchingOptions.CloseYearBonus);
            result.Confidence.Should().Be(YearMatchConfidence.High);
        }

        [Test]
        public void should_apply_penalty_for_two_year_difference()
        {
            var result = Subject.Match(new DateTime(2020, 6, 15), 2022);

            result.IsMatch.Should().BeTrue();
            result.YearDifference.Should().Be(2);
            result.ScoreAdjustment.Should().BeLessThan(0);
        }

        [Test]
        public void should_apply_increasing_penalty_for_larger_differences()
        {
            var result3 = Subject.Match(new DateTime(2020, 6, 15), 2023);
            var result4 = Subject.Match(new DateTime(2020, 6, 15), 2024);

            result3.ScoreAdjustment.Should().BeGreaterThan(result4.ScoreAdjustment);
        }

        [Test]
        public void should_reject_when_year_difference_exceeds_hard_limit()
        {
            var result = Subject.Match(new DateTime(2020, 6, 15), 2010);

            result.IsMatch.Should().BeFalse();
            result.YearDifference.Should().Be(10);
            result.RejectionReason.Should().Be("Release year 2010 does not match album year 2020 (10 years apart)");
        }

        [Test]
        public void should_reject_at_boundary_of_hard_limit()
        {
            var result = Subject.Match(new DateTime(2020, 6, 15), 2014);

            result.IsMatch.Should().BeFalse();
            result.YearDifference.Should().Be(6);
        }

        [Test]
        public void should_accept_at_fuzzy_match_boundary()
        {
            var result = Subject.Match(new DateTime(2020, 6, 15), 2015);

            result.IsMatch.Should().BeTrue();
            result.YearDifference.Should().Be(5);
            result.Confidence.Should().Be(YearMatchConfidence.Low);
        }

        [TestCase(2020, 2020, true)]
        [TestCase(2020, 2019, true)]
        [TestCase(2020, 2021, true)]
        [TestCase(2020, 2018, true)]
        [TestCase(2020, 2023, true)]
        [TestCase(2020, 2025, true)]
        [TestCase(2020, 2026, false)]
        [TestCase(2020, 2014, false)]
        [TestCase(2020, 2010, false)]
        public void should_handle_year_boundaries_correctly(int albumYear, int parsedYear, bool expectedMatch)
        {
            var result = Subject.Match(new DateTime(albumYear, 1, 1), parsedYear);

            result.IsMatch.Should().Be(expectedMatch);
        }

        [Test]
        public void calculate_year_score_should_return_bonus_for_exact_match()
        {
            var score = Subject.CalculateYearScore(new DateTime(2020, 1, 1), 2020);

            score.Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
        }

        [Test]
        public void calculate_year_score_should_return_zero_when_no_year_provided()
        {
            var score = Subject.CalculateYearScore(new DateTime(2020, 1, 1), null);

            score.Should().Be(0);
        }

        [Test]
        public void calculate_year_score_should_return_zero_when_no_album_date()
        {
            var score = Subject.CalculateYearScore(null, 2020);

            score.Should().Be(0);
        }

        [Test]
        public void should_match_album_with_null_year()
        {
            var album = new Album { Title = "Test", ReleaseDate = new DateTime(2020, 1, 1) };

            var result = Subject.Match(album, null);

            result.IsMatch.Should().BeTrue();
        }

        [Test]
        public void should_match_album_with_exact_year()
        {
            var album = new Album { Title = "Test", ReleaseDate = new DateTime(2020, 1, 1) };

            var result = Subject.Match(album, 2020);

            result.IsMatch.Should().BeTrue();
            result.ScoreAdjustment.Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
        }

        [Test]
        public void should_be_lenient_for_compilation_albums()
        {
            var album = new Album
            {
                Title = "Greatest Hits",
                ReleaseDate = new DateTime(2020, 1, 1),
                SecondaryTypes = new List<SecondaryAlbumType>
                {
                    new SecondaryAlbumType { Name = "Compilation" }
                },
                AlbumReleases = new LazyLoaded<List<AlbumRelease>>(new List<AlbumRelease>())
            };

            var result = Subject.Match(album, 2015);

            result.IsMatch.Should().BeTrue();
        }

        [Test]
        public void should_be_lenient_for_live_albums()
        {
            var album = new Album
            {
                Title = "Live at Wembley",
                ReleaseDate = new DateTime(2020, 1, 1),
                SecondaryTypes = new List<SecondaryAlbumType>
                {
                    new SecondaryAlbumType { Name = "Live" }
                },
                AlbumReleases = new LazyLoaded<List<AlbumRelease>>(new List<AlbumRelease>())
            };

            var result = Subject.Match(album, 2015);

            result.IsMatch.Should().BeTrue();
        }

        [TestCase("Woodstock 1969", 2009, 1969)]
        [TestCase("1989", 2014, 1989)]
        [TestCase("Live 1975", 2022, 1975)]
        [TestCase("1988 Rewind Edition", 2023, 1988)]
        public void should_match_when_parsed_year_is_part_of_album_title(string title, int albumYear, int parsedYear)
        {
            var album = new Album { Title = title, ReleaseDate = new DateTime(albumYear, 6, 15) };

            var result = Subject.Match(album, parsedYear);

            result.IsMatch.Should().BeTrue();
            result.ScoreAdjustment.Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
        }

        [TestCase("Album 19690")]
        [TestCase("SR119694")]
        [TestCase("Now That's What I Call Music! 2023")]
        public void should_not_match_when_year_is_not_standalone_in_album_title(string title)
        {
            var album = new Album { Title = title, ReleaseDate = new DateTime(2023, 6, 15) };

            var result = Subject.Match(album, 1969);

            result.IsMatch.Should().BeFalse();
        }

        [Test]
        public void should_not_fail_when_album_title_is_null()
        {
            var album = new Album { Title = null, ReleaseDate = new DateTime(2020, 6, 15) };

            var result = Subject.Match(album, 2010);

            result.IsMatch.Should().BeFalse();
        }

        [Test]
        public void calculate_album_year_score_should_return_bonus_when_year_is_part_of_album_title()
        {
            var album = new Album { Title = "Woodstock 1969", ReleaseDate = new DateTime(2009, 6, 15) };

            Subject.CalculateAlbumYearScore(album, 1969).Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
        }

        [Test]
        public void calculate_album_year_score_should_use_release_date_when_year_is_not_in_album_title()
        {
            var album = new Album { Title = "Woodstock", ReleaseDate = new DateTime(2009, 6, 15) };

            Subject.CalculateAlbumYearScore(album, 2009).Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
            Subject.CalculateAlbumYearScore(album, 1969).Should().BeLessThan(0);
        }

        private static Album AlbumWithEditions(DateTime? originalDate, params DateTime?[] editionDates)
        {
            return new Album
            {
                Title = "Test",
                ReleaseDate = originalDate,
                AlbumReleases = new LazyLoaded<List<AlbumRelease>>(editionDates.Select(d => new AlbumRelease { ReleaseDate = d, Monitored = true }).ToList())
            };
        }

        [Test]
        public void should_match_unmonitored_edition_year()
        {
            var album = AlbumWithEditions(new DateTime(1973, 3, 1), new DateTime(2011, 9, 26));
            album.AlbumReleases.Value.ForEach(r => r.Monitored = false);

            var result = Subject.Match(album, 2011);

            result.IsMatch.Should().BeTrue();
            result.ScoreAdjustment.Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
        }

        [Test]
        public void should_use_same_tolerance_for_edition_years_as_for_original_year()
        {
            var album = AlbumWithEditions(new DateTime(1990, 1, 1), new DateTime(2020, 1, 1));

            var result = Subject.Match(album, 2022);

            result.IsMatch.Should().BeTrue();
            result.YearDifference.Should().Be(2);
        }

        [Test]
        public void should_pick_best_edition_year_regardless_of_order()
        {
            var album = AlbumWithEditions(new DateTime(1990, 1, 1), new DateTime(2019, 1, 1), new DateTime(2020, 1, 1));

            var result = Subject.Match(album, 2020);

            result.YearDifference.Should().Be(0);
            result.ScoreAdjustment.Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
        }

        [Test]
        public void should_not_let_undated_edition_bypass_year_check()
        {
            var album = AlbumWithEditions(new DateTime(1990, 1, 1), (DateTime?)null);

            var result = Subject.Match(album, 2020);

            result.IsMatch.Should().BeFalse();
        }

        [Test]
        public void should_match_dated_edition_when_original_date_is_unknown()
        {
            var album = AlbumWithEditions(null, new DateTime(2020, 1, 1));

            var result = Subject.Match(album, 2020);

            result.ScoreAdjustment.Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
        }

        [Test]
        public void should_reject_when_original_date_is_unknown_and_editions_are_far_off()
        {
            var album = AlbumWithEditions(null, new DateTime(2000, 1, 1));

            var result = Subject.Match(album, 2020);

            result.IsMatch.Should().BeFalse();
        }

        [Test]
        public void should_report_closest_year_in_rejection_reason()
        {
            var album = AlbumWithEditions(new DateTime(1973, 3, 1), new DateTime(2003, 1, 1));

            var result = Subject.Match(album, 2011);

            result.RejectionReason.Should().Be("Release year 2011 does not match album year 2003 (8 years apart)");
        }

        [Test]
        public void should_keep_exact_bonus_for_live_album_with_exact_year()
        {
            var album = new Album
            {
                Title = "Live at Wembley",
                ReleaseDate = new DateTime(2020, 1, 1),
                SecondaryTypes = new List<SecondaryAlbumType> { new SecondaryAlbumType { Name = "Live" } },
                AlbumReleases = new LazyLoaded<List<AlbumRelease>>(new List<AlbumRelease>())
            };

            var result = Subject.Match(album, 2020);

            result.ScoreAdjustment.Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
            result.Confidence.Should().Be(YearMatchConfidence.High);
        }

        [Test]
        public void calculate_album_year_score_should_use_edition_years()
        {
            var album = AlbumWithEditions(new DateTime(1973, 3, 1), new DateTime(2011, 9, 26));

            Subject.CalculateAlbumYearScore(album, 2011).Should().Be(AlbumYearMatchingOptions.ExactYearBonus);
        }

        [Test]
        public void should_keep_year_distance_score_for_compilations()
        {
            var compilation2010 = new Album
            {
                Title = "Greatest Hits",
                ReleaseDate = new DateTime(2010, 1, 1),
                SecondaryTypes = new List<SecondaryAlbumType> { new SecondaryAlbumType { Name = "Compilation" } },
                AlbumReleases = new LazyLoaded<List<AlbumRelease>>(new List<AlbumRelease>())
            };

            var compilation2013 = new Album
            {
                Title = "Greatest Hits",
                ReleaseDate = new DateTime(2013, 1, 1),
                SecondaryTypes = new List<SecondaryAlbumType> { new SecondaryAlbumType { Name = "Compilation" } },
                AlbumReleases = new LazyLoaded<List<AlbumRelease>>(new List<AlbumRelease>())
            };

            Subject.CalculateAlbumYearScore(compilation2013, 2015).Should().BeGreaterThan(Subject.CalculateAlbumYearScore(compilation2010, 2015));
        }
    }
}
