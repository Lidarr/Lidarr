using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class TrackServiceFixture : CoreTest<TrackService>
    {
        [Test]
        public void should_update_matching_recordings_across_album_releases()
        {
            var album = new Album { Id = 10 };
            var release = new AlbumRelease
            {
                Id = 20,
                AlbumId = album.Id,
                Album = album
            };

            var selectedTrack = new Track
            {
                Id = 1,
                ForeignRecordingId = "recording",
                AlbumRelease = release
            };

            var siblingTrack = new Track
            {
                Id = 2,
                ForeignRecordingId = selectedTrack.ForeignRecordingId
            };

            Mocker.GetMock<ITrackRepository>()
                  .Setup(repository => repository.Get(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Track> { selectedTrack });

            Mocker.GetMock<ITrackRepository>()
                  .Setup(repository => repository.GetTracksByAlbumAndRecordingIds(
                      album.Id,
                      It.Is<List<string>>(ids => ids.SequenceEqual(new[] { selectedTrack.ForeignRecordingId }))))
                  .Returns(new List<Track> { selectedTrack, siblingTrack });

            var result = Subject.SetMonitored(new[] { selectedTrack.Id }, false);

            result.Should().HaveCount(2);
            result.Should().OnlyContain(track => !track.Monitored);
            Mocker.GetMock<ITrackRepository>()
                  .Verify(repository => repository.SetMonitored(
                      It.Is<List<Track>>(tracks => tracks.Count == 2 && tracks.All(track => !track.Monitored))),
                      Times.Once());
            Mocker.GetMock<IEventAggregator>()
                  .Verify(eventAggregator => eventAggregator.PublishEvent(It.Is<AlbumEditedEvent>(eventArgs => eventArgs.Album.Id == album.Id)), Times.Once());
        }
    }
}
