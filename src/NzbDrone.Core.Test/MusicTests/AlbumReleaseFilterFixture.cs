using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class AlbumReleaseFilterFixture : CoreTest
    {
        private Album _album;
        private AlbumRelease _elevenTrackSingleDisc;
        private AlbumRelease _twelveTrackSingleDisc;
        private AlbumRelease _twentyTwoTrackDoubleDisc;

        private static AlbumRelease CreateRelease(int id, int trackCount, int mediumCount, bool monitored = false)
        {
            return new AlbumRelease
            {
                Id = id,
                TrackCount = trackCount,
                Monitored = monitored,
                Media = Enumerable.Range(1, mediumCount).Select(n => new Medium { Number = n, Format = "CD" }).ToList()
            };
        }

        [SetUp]
        public void Setup()
        {
            _elevenTrackSingleDisc = CreateRelease(1, 11, 1);
            _twelveTrackSingleDisc = CreateRelease(2, 12, 1);
            _twentyTwoTrackDoubleDisc = CreateRelease(3, 22, 2, monitored: true);

            _album = new Album
            {
                Id = 10,
                AnyReleaseOk = true,
                AlbumReleases = new List<AlbumRelease> { _elevenTrackSingleDisc, _twelveTrackSingleDisc, _twentyTwoTrackDoubleDisc }
            };
        }

        [Test]
        public void should_have_no_filter_by_default()
        {
            _album.HasReleaseFilter().Should().BeFalse();
            _album.GetAllowedReleases().Should().HaveCount(3);
        }

        [Test]
        public void should_count_only_audio_media()
        {
            var release = CreateRelease(9, 10, 1);
            release.Media.Add(new Medium { Number = 0, Format = "DVD" });

            release.AudioMediumCount().Should().Be(1);
        }

        [Test]
        public void should_filter_by_track_count()
        {
            _album.PreferredTrackCounts = new List<int> { 11, 12 };

            _album.GetAllowedReleases().Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 2 });
            _album.IsAllowedRelease(_twentyTwoTrackDoubleDisc).Should().BeFalse();
        }

        [Test]
        public void should_filter_by_medium_count()
        {
            _album.PreferredMediumCounts = new List<int> { 2 };

            _album.GetAllowedReleases().Select(x => x.Id).Should().BeEquivalentTo(new[] { 3 });
        }

        [Test]
        public void should_require_both_filters_to_match()
        {
            _album.PreferredTrackCounts = new List<int> { 11, 22 };
            _album.PreferredMediumCounts = new List<int> { 1 };

            _album.GetAllowedReleases().Select(x => x.Id).Should().BeEquivalentTo(new[] { 1 });
        }

        [Test]
        public void should_fall_back_to_all_releases_when_filter_matches_nothing()
        {
            _album.PreferredTrackCounts = new List<int> { 99 };

            _album.GetAllowedReleases().Should().HaveCount(3);
        }

        [Test]
        public void monitored_release_should_always_be_wanted()
        {
            _album.PreferredTrackCounts = new List<int> { 11 };

            _album.IsReleaseWanted(_twentyTwoTrackDoubleDisc).Should().BeTrue();
        }

        [Test]
        public void allowed_release_should_be_wanted_only_when_any_release_ok()
        {
            _album.PreferredTrackCounts = new List<int> { 11 };

            _album.IsReleaseWanted(_elevenTrackSingleDisc).Should().BeTrue();
            _album.IsReleaseWanted(_twelveTrackSingleDisc).Should().BeFalse();

            _album.AnyReleaseOk = false;
            _album.IsReleaseWanted(_elevenTrackSingleDisc).Should().BeFalse();
        }

        [Test]
        public void should_keep_monitored_release_when_it_is_allowed()
        {
            _album.PreferredMediumCounts = new List<int> { 2 };

            _album.ChooseMonitoredRelease(_album.AlbumReleases.Value).Should().BeSameAs(_twentyTwoTrackDoubleDisc);
        }

        [Test]
        public void should_choose_allowed_release_with_most_tracks_when_monitored_is_excluded()
        {
            _album.PreferredMediumCounts = new List<int> { 1 };

            _album.ChooseMonitoredRelease(_album.AlbumReleases.Value).Should().BeSameAs(_twelveTrackSingleDisc);
        }

        [Test]
        public void should_prefer_release_with_files_on_disk()
        {
            _album.PreferredMediumCounts = new List<int> { 1 };

            var chosen = _album.ChooseMonitoredRelease(_album.AlbumReleases.Value, x => x.Id == 1 ? 5 : 0);

            chosen.Should().BeSameAs(_elevenTrackSingleDisc);
        }

        [Test]
        public void apply_changes_should_copy_filter_and_blocklist_flag()
        {
            var other = new Album
            {
                PreferredTrackCounts = new List<int> { 11 },
                PreferredMediumCounts = new List<int> { 1 },
                AutoBlocklistUnwantedRelease = true
            };

            _album.ApplyChanges(other);

            _album.PreferredTrackCounts.Should().Equal(11);
            _album.PreferredMediumCounts.Should().Equal(1);
            _album.AutoBlocklistUnwantedRelease.Should().BeTrue();

            _album.ApplyChanges(new Album { PreferredTrackCounts = null, PreferredMediumCounts = null });

            _album.PreferredTrackCounts.Should().BeEmpty();
            _album.PreferredMediumCounts.Should().BeEmpty();
        }

        [Test]
        public void should_not_fail_for_null_album()
        {
            Album album = null;

            album.HasReleaseFilter().Should().BeFalse();
            album.IsAllowedRelease(_elevenTrackSingleDisc).Should().BeTrue();
            album.IsReleaseWanted(_elevenTrackSingleDisc).Should().BeFalse();
            album.ChooseMonitoredRelease(_album.AlbumReleases.Value).Should().BeSameAs(_twentyTwoTrackDoubleDisc);
        }
    }
}
