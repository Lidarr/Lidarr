using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.TrackImport.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.TrackImport.Specifications
{
    [TestFixture]
    public class ReleaseWantedSpecificationFixture : CoreTest<ReleaseWantedSpecification>
    {
        private Album _album;
        private AlbumRelease _monitoredRelease;
        private AlbumRelease _otherRelease;

        private static AlbumRelease CreateRelease(int id, int trackCount, int mediumCount, bool monitored)
        {
            return new AlbumRelease
            {
                Id = id,
                TrackCount = trackCount,
                Monitored = monitored,
                Media = Enumerable.Range(1, mediumCount).Select(n => new Medium { Number = n }).ToList()
            };
        }

        private static LocalAlbumRelease LocalRelease(AlbumRelease release)
        {
            return new LocalAlbumRelease(new List<LocalTrack>()) { AlbumRelease = release };
        }

        [SetUp]
        public void Setup()
        {
            _monitoredRelease = CreateRelease(1, 22, 2, true);
            _otherRelease = CreateRelease(2, 11, 1, false);

            _album = new Album
            {
                Id = 5,
                AnyReleaseOk = true,
                AlbumReleases = new List<AlbumRelease> { _monitoredRelease, _otherRelease }
            };

            _monitoredRelease.Album = _album;
            _otherRelease.Album = _album;
        }

        [Test]
        public void should_accept_monitored_release()
        {
            Subject.IsSatisfiedBy(LocalRelease(_monitoredRelease), null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_any_release_when_any_release_ok_and_no_filter()
        {
            Subject.IsSatisfiedBy(LocalRelease(_otherRelease), null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_unmonitored_release_when_any_release_not_ok()
        {
            _album.AnyReleaseOk = false;

            Subject.IsSatisfiedBy(LocalRelease(_otherRelease), null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_release_excluded_by_filter()
        {
            _album.PreferredMediumCounts = new List<int> { 2 };

            var decision = Subject.IsSatisfiedBy(LocalRelease(_otherRelease), null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Contain("filter");
        }

        [Test]
        public void should_accept_release_allowed_by_filter()
        {
            _album.PreferredTrackCounts = new List<int> { 11 };

            Subject.IsSatisfiedBy(LocalRelease(_otherRelease), null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_still_accept_monitored_release_excluded_by_filter()
        {
            _album.PreferredTrackCounts = new List<int> { 11 };

            Subject.IsSatisfiedBy(LocalRelease(_monitoredRelease), null).Accepted.Should().BeTrue();
        }
    }
}
