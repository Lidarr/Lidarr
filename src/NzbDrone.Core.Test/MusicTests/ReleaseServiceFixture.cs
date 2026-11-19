using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class ReleaseServiceFixture : CoreTest<ReleaseService>
    {
        private Album _album;
        private AlbumRelease _single11;
        private AlbumRelease _single12;
        private AlbumRelease _double22;

        private static AlbumRelease CreateRelease(int id, int trackCount, int mediumCount, bool monitored)
        {
            return new AlbumRelease
            {
                Id = id,
                AlbumId = 1,
                TrackCount = trackCount,
                Monitored = monitored,
                Media = Enumerable.Range(1, mediumCount).Select(n => new Medium { Number = n }).ToList()
            };
        }

        [SetUp]
        public void Setup()
        {
            _single11 = CreateRelease(1, 11, 1, false);
            _single12 = CreateRelease(2, 12, 1, false);
            _double22 = CreateRelease(3, 22, 2, true);

            _album = new Album
            {
                Id = 1,
                AnyReleaseOk = true,
                AlbumReleases = new List<AlbumRelease> { _single11, _single12, _double22 }
            };
        }

        private List<int> MonitoredIds()
        {
            return _album.AlbumReleases.Value.Where(x => x.Monitored).Select(x => x.Id).ToList();
        }

        [Test]
        public void should_do_nothing_when_album_has_no_releases()
        {
            _album.AlbumReleases = new List<AlbumRelease>();

            Subject.ApplyReleaseFilter(_album, null);

            _album.AlbumReleases.Value.Should().BeEmpty();
        }

        [Test]
        public void should_keep_monitored_release_when_no_filter()
        {
            Subject.ApplyReleaseFilter(_album, 3);

            MonitoredIds().Should().Equal(3);
        }

        [Test]
        public void should_throw_when_filter_matches_no_release()
        {
            _album.PreferredTrackCounts = new List<int> { 22 };
            _album.PreferredMediumCounts = new List<int> { 1 };

            Assert.Throws<NzbDroneClientException>(() => Subject.ApplyReleaseFilter(_album, 3));
        }

        [Test]
        public void should_switch_monitored_release_when_current_is_excluded()
        {
            _album.PreferredMediumCounts = new List<int> { 1 };

            Subject.ApplyReleaseFilter(_album, 3);

            MonitoredIds().Should().Equal(2);
        }

        [Test]
        public void should_keep_monitored_release_when_it_is_allowed()
        {
            _album.PreferredTrackCounts = new List<int> { 22 };

            Subject.ApplyReleaseFilter(_album, 3);

            MonitoredIds().Should().Equal(3);
        }

        [Test]
        public void should_respect_explicit_user_pick_even_when_excluded()
        {
            _album.PreferredTrackCounts = new List<int> { 11 };

            // user switched monitored release from 1 to 3 in this request
            Subject.ApplyReleaseFilter(_album, 1);

            MonitoredIds().Should().Equal(3);
        }

        [Test]
        public void should_pick_a_release_when_none_monitored()
        {
            _album.AlbumReleases.Value.ForEach(x => x.Monitored = false);
            _album.PreferredTrackCounts = new List<int> { 11, 12 };

            Subject.ApplyReleaseFilter(_album, null);

            MonitoredIds().Should().Equal(2);
        }
    }
}
