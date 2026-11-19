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
        public void should_accept_unmonitored_songs_when_the_whole_album_was_requested()
        {
            var track = new LocalTrack { Tracks = new List<Track> { new Track { Monitored = false } } };

            Subject.IsSatisfiedBy(track, new DownloadClientItem { IsFullAlbumRequest = true }).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_still_limit_a_targeted_download_even_if_it_carries_the_full_album_flag()
        {
            var track = new LocalTrack { Tracks = new List<Track> { new Track { Monitored = false, ForeignRecordingId = "other" } } };
            var download = new DownloadClientItem { IsFullAlbumRequest = true, TargetRecordingIds = new List<string> { "wanted" } };

            Subject.IsSatisfiedBy(track, download).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_not_treat_unknown_downloads_as_whole_album_requests()
        {
            var track = new LocalTrack { Tracks = new List<Track> { new Track { Monitored = false } } };

            Subject.IsSatisfiedBy(track, new DownloadClientItem()).Accepted.Should().BeFalse();
        }

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
