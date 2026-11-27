using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.ArtistServiceTests
{
    [TestFixture]
    public class SongModeSelectionFixture : CoreTest<ArtistService>
    {
        private Artist _stored;
        private Artist _updated;
        private List<Track> _tracks;

        [SetUp]
        public void Setup()
        {
            _stored = new Artist { Id = 1, ArtistMetadataId = 2 };
            _updated = new Artist { Id = 1, ArtistMetadataId = 2, SongMode = true };
            _tracks = new List<Track>
            {
                new Track { Id = 10, Monitored = true, TrackFileId = 100 },
                new Track { Id = 11, Monitored = true }
            };
            Mocker.GetMock<IArtistRepository>().Setup(repository => repository.Get(1)).Returns(_stored);
            Mocker.GetMock<IArtistRepository>().Setup(repository => repository.Update(_updated)).Returns(_updated);
            Mocker.GetMock<ITrackRepository>().Setup(repository => repository.GetTracks(1)).Returns(_tracks);
        }

        [Test]
        public void should_start_with_no_wanted_songs_when_enabling_from_album_defaults()
        {
            Subject.UpdateArtist(_updated);

            Mocker.GetMock<ITrackRepository>().Verify(repository => repository.UnmonitorArtist(2), Times.Once());
            _tracks[0].TrackFileId.Should().Be(100);
        }

        [Test]
        public void should_preserve_a_partial_song_selection_when_reentering_song_mode()
        {
            _tracks[1].Monitored = false;

            Subject.UpdateArtist(_updated);

            Mocker.GetMock<ITrackRepository>().Verify(repository => repository.UnmonitorArtist(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_preserve_an_intentional_select_all_when_updating_an_artist_already_in_song_mode()
        {
            _stored.SongMode = true;

            Subject.UpdateArtist(_updated);

            Mocker.GetMock<ITrackRepository>().Verify(repository => repository.UnmonitorArtist(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_not_change_song_monitoring_when_switching_to_album_mode()
        {
            _stored.SongMode = true;
            _updated.SongMode = false;

            Subject.UpdateArtist(_updated);

            Mocker.GetMock<ITrackRepository>().Verify(repository => repository.UnmonitorArtist(It.IsAny<int>()), Times.Never());
        }
    }
}
