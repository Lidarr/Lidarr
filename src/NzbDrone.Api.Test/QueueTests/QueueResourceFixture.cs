using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Lidarr.Api.V1.Queue;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;
using Queue = NzbDrone.Core.Queue.Queue;

namespace NzbDrone.Api.Test.QueueTests
{
    [TestFixture]
    public class QueueResourceFixture : TestBase
    {
        private Queue _queue;
        private AlbumRelease _release;

        [SetUp]
        public void Setup()
        {
            _release = new AlbumRelease
            {
                Monitored = true,
                Tracks = new List<Track>
                {
                    new Track { ForeignRecordingId = "one", Title = "Song One", Monitored = false },
                    new Track { ForeignRecordingId = "two", Title = "Song Two" },
                    new Track { ForeignRecordingId = "three", Title = "Song Three" }
                }
            };
            var album = new Album { Id = 1, AlbumReleases = new List<AlbumRelease> { _release } };
            _queue = new Queue
            {
                Status = "Downloading",
                Album = album,
                RemoteAlbum = new RemoteAlbum { Albums = new List<Album> { album } }
            };
        }

        [Test]
        public void whole_album_download_should_not_have_target_songs()
        {
            _queue.ToResource(false, false).TargetTracks.Should().BeEmpty();
        }

        [Test]
        public void should_show_only_grabbed_recordings_not_current_monitored_songs()
        {
            _queue.RemoteAlbum.TargetRecordingIds = new List<string> { "one", "three" };
            var resource = _queue.ToResource(false, false);
            resource.TargetTracks.Should().HaveCount(2);
            resource.TargetTracks.Select(track => track.Title).Should().Equal("Song One", "Song Three");
        }

        [Test]
        public void should_deduplicate_recordings()
        {
            _queue.RemoteAlbum.TargetRecordingIds = new List<string> { "one", "one" };
            _queue.ToResource(false, false).TargetTracks.Should().ContainSingle();
        }

        [Test]
        public void should_resolve_recordings_from_other_editions()
        {
            _queue.Album.AlbumReleases.Value.Add(new AlbumRelease
            {
                Tracks = new List<Track> { new Track { ForeignRecordingId = "bonus", Title = "Bonus Song" } }
            });
            _queue.RemoteAlbum.TargetRecordingIds = new List<string> { "bonus" };
            _queue.ToResource(false, false).TargetTracks[0].Title.Should().Be("Bonus Song");
        }

        [Test]
        public void should_prefer_title_from_monitored_edition()
        {
            _queue.Album.AlbumReleases.Value.Insert(0, new AlbumRelease
            {
                Tracks = new List<Track> { new Track { ForeignRecordingId = "one", Title = "Alternate Title" } }
            });
            _queue.RemoteAlbum.TargetRecordingIds = new List<string> { "one" };
            _queue.ToResource(false, false).TargetTracks[0].Title.Should().Be("Song One");
        }

        [Test]
        public void should_keep_unresolved_targets_when_metadata_is_unavailable()
        {
            _queue.Album = null;
            _queue.RemoteAlbum.Albums.Clear();
            _queue.RemoteAlbum.TargetRecordingIds = new List<string> { "missing" };
            var target = _queue.ToResource(false, false).TargetTracks.Should().ContainSingle().Subject;
            target.ForeignRecordingId.Should().Be("missing");
            target.Title.Should().BeNull();
        }

        [Test]
        public void should_handle_unknown_download_without_remote_album()
        {
            _queue.RemoteAlbum = null;
            _queue.ToResource(false, false).TargetTracks.Should().BeEmpty();
        }
    }
}
