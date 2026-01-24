using System;
using System.Collections.Generic;
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
            result.RejectionReason.Should().Contain("does not match");
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
    }
}
