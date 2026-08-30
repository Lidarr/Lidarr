using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.TrackImport.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.TrackImport.Specifications
{
    [TestFixture]
    public class MonitoredTrackSpecificationFixture : CoreTest<MonitoredTrackSpecification>
    {
        [Test]
        public void should_accept_monitored_track()
        {
            var localTrack = new LocalTrack
            {
                Tracks = new List<Track> { new Track { Monitored = true } }
            };

            Subject.IsSatisfiedBy(localTrack, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_unmonitored_track_from_download()
        {
            var localTrack = new LocalTrack
            {
                Tracks = new List<Track> { new Track { Monitored = false } }
            };

            Subject.IsSatisfiedBy(localTrack, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_unmonitored_existing_file()
        {
            var localTrack = new LocalTrack
            {
                ExistingFile = true,
                Tracks = new List<Track> { new Track { Monitored = false } }
            };

            Subject.IsSatisfiedBy(localTrack, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_track_targeted_by_download()
        {
            var localTrack = new LocalTrack
            {
                Tracks = new List<Track> { new Track { ForeignRecordingId = "target", Monitored = false } }
            };
            var downloadClientItem = new DownloadClientItem
            {
                TargetRecordingIds = new List<string> { "target" }
            };

            Subject.IsSatisfiedBy(localTrack, downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_monitored_track_not_targeted_by_download()
        {
            var localTrack = new LocalTrack
            {
                Tracks = new List<Track> { new Track { ForeignRecordingId = "other", Monitored = true } }
            };
            var downloadClientItem = new DownloadClientItem
            {
                TargetRecordingIds = new List<string> { "target" }
            };

            Subject.IsSatisfiedBy(localTrack, downloadClientItem).Accepted.Should().BeFalse();
        }
    }
}
