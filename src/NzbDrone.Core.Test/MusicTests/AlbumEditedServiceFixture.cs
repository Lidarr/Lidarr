using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class AlbumEditedServiceFixture : CoreTest<AlbumEditedService>
    {
        private Album _oldAlbum;
        private Album _newAlbum;

        private Album CreateAlbum()
        {
            var artist = Builder<Artist>.CreateNew().With(x => x.Path = @"C:\Music\Artist").Build();
            var releases = new List<AlbumRelease>
            {
                new AlbumRelease { Id = 1, Monitored = true, TrackCount = 11 },
                new AlbumRelease { Id = 2, Monitored = false, TrackCount = 22 }
            };

            return new Album
            {
                Id = 5,
                AnyReleaseOk = true,
                Artist = artist,
                AlbumReleases = releases
            };
        }

        [SetUp]
        public void Setup()
        {
            _oldAlbum = CreateAlbum();
            _newAlbum = CreateAlbum();

            Mocker.GetMock<ITrackService>()
                .Setup(x => x.GetTracksByAlbum(5))
                .Returns(new List<Track> { new Track { Id = 1, TrackFileId = 10 } });
        }

        private void AssertRescan(Times times)
        {
            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.IsAny<RescanFoldersCommand>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()), times);
        }

        [Test]
        public void should_not_rescan_when_nothing_relevant_changed()
        {
            Subject.Handle(new AlbumEditedEvent(_newAlbum, _oldAlbum));

            AssertRescan(Times.Never());
        }

        [Test]
        public void should_rescan_when_track_count_filter_changed()
        {
            _newAlbum.PreferredTrackCounts = new List<int> { 11 };

            Subject.Handle(new AlbumEditedEvent(_newAlbum, _oldAlbum));

            AssertRescan(Times.Once());

            Mocker.GetMock<ITrackService>()
                .Verify(x => x.SetFileIds(It.Is<List<Track>>(t => t.TrueForAll(y => y.TrackFileId == 0))), Times.Once());
        }

        [Test]
        public void should_rescan_when_medium_count_filter_changed()
        {
            _oldAlbum.PreferredMediumCounts = new List<int> { 1, 2 };
            _newAlbum.PreferredMediumCounts = new List<int> { 1 };

            Subject.Handle(new AlbumEditedEvent(_newAlbum, _oldAlbum));

            AssertRescan(Times.Once());
        }

        [Test]
        public void should_not_rescan_when_filter_order_differs_but_content_is_same()
        {
            _oldAlbum.PreferredTrackCounts = new List<int> { 12, 11 };
            _newAlbum.PreferredTrackCounts = new List<int> { 11, 12 };

            Subject.Handle(new AlbumEditedEvent(_newAlbum, _oldAlbum));

            AssertRescan(Times.Never());
        }

        [Test]
        public void should_rescan_when_monitored_release_changed()
        {
            _newAlbum.AlbumReleases.Value[0].Monitored = false;
            _newAlbum.AlbumReleases.Value[1].Monitored = true;

            Subject.Handle(new AlbumEditedEvent(_newAlbum, _oldAlbum));

            AssertRescan(Times.Once());
        }
    }
}
