using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.AlbumServiceTests
{
    [TestFixture]
    public class UpdateAlbumFixture : CoreTest<AlbumService>
    {
        private Album _album;
        private AlbumRelease _monitoredRelease;
        private AlbumEditedEvent _albumEditedEvent;

        [SetUp]
        public void Setup()
        {
            // The album as the edit dialog sends it: releases without stored details, release 2 chosen
            _album = new Album
            {
                Id = 10,
                Title = "Pyromania",
                AlbumReleases = new List<AlbumRelease>
                {
                    new AlbumRelease { Id = 1, AlbumId = 10, Monitored = false },
                    new AlbumRelease { Id = 2, AlbumId = 10, Monitored = true }
                }
            };

            Mocker.GetMock<IAlbumRepository>()
                .Setup(s => s.Get(_album.Id))
                .Returns(() => new Album { Id = _album.Id, Title = _album.Title, AlbumReleases = GivenStoredReleases() });

            Mocker.GetMock<IAlbumRepository>()
                .Setup(s => s.Update(It.IsAny<Album>()))
                .Returns<Album>(a => a);

            Mocker.GetMock<IReleaseService>()
                .Setup(s => s.GetReleasesByAlbum(_album.Id))
                .Returns(GivenStoredReleases);

            Mocker.GetMock<IReleaseService>()
                .Setup(s => s.SetMonitored(It.IsAny<AlbumRelease>()))
                .Callback<AlbumRelease>(r => _monitoredRelease = r);

            Mocker.GetMock<IEventAggregator>()
                .Setup(s => s.PublishEvent(It.IsAny<AlbumEditedEvent>()))
                .Callback<AlbumEditedEvent>(e => _albumEditedEvent = e);
        }

        private static List<AlbumRelease> GivenStoredReleases()
        {
            return new List<AlbumRelease>
            {
                new AlbumRelease
                {
                    Id = 1,
                    AlbumId = 10,
                    Title = "Pyromania",
                    ReleaseDate = new DateTime(1983, 1, 20),
                    OldForeignReleaseIds = new List<string> { "old-release-1" },
                    Monitored = true
                },
                new AlbumRelease
                {
                    Id = 2,
                    AlbumId = 10,
                    Title = "Pyromania",
                    ReleaseDate = new DateTime(1983, 3, 1),
                    OldForeignReleaseIds = new List<string> { "old-release-2" },
                    Monitored = false
                }
            };
        }

        private void GivenMonitored(params int[] releaseIds)
        {
            _album.AlbumReleases.Value.ForEach(r => r.Monitored = releaseIds.Contains(r.Id));
        }

        private void VerifyNoReleasesWritten()
        {
            Mocker.GetMock<IReleaseService>().Verify(v => v.SetMonitored(It.IsAny<AlbumRelease>()), Times.Never());
            Mocker.GetMock<IReleaseService>().Verify(v => v.UpdateMany(It.IsAny<List<AlbumRelease>>()), Times.Never());
        }

        [Test]
        public void should_set_chosen_release_as_monitored()
        {
            Subject.UpdateAlbum(_album);

            Mocker.GetMock<IReleaseService>().Verify(v => v.SetMonitored(It.Is<AlbumRelease>(r => r.Id == 2)), Times.Once());
        }

        [Test]
        public void should_keep_release_dates_and_old_release_ids_when_release_is_chosen()
        {
            Subject.UpdateAlbum(_album);

            _monitoredRelease.ReleaseDate.Should().Be(new DateTime(1983, 3, 1));
            _monitoredRelease.OldForeignReleaseIds.Should().Equal("old-release-2");
            Mocker.GetMock<IReleaseService>().Verify(v => v.UpdateMany(It.IsAny<List<AlbumRelease>>()), Times.Never());
        }

        [Test]
        public void should_not_write_releases_when_monitored_release_is_unchanged()
        {
            GivenMonitored(1);

            Subject.UpdateAlbum(_album);

            VerifyNoReleasesWritten();
        }

        [Test]
        public void should_not_write_releases_when_releases_are_not_loaded()
        {
            _album.AlbumReleases = new LazyLoaded<List<AlbumRelease>>();

            Subject.UpdateAlbum(_album);

            Mocker.GetMock<IAlbumRepository>().Verify(v => v.Update(_album), Times.Once());
            Mocker.GetMock<IReleaseService>().Verify(v => v.GetReleasesByAlbum(It.IsAny<int>()), Times.Never());
            VerifyNoReleasesWritten();
        }

        [Test]
        public void should_not_change_releases_when_chosen_release_is_not_on_album()
        {
            _album.AlbumReleases = new List<AlbumRelease> { new AlbumRelease { Id = 99, Monitored = true } };

            Subject.UpdateAlbum(_album);

            VerifyNoReleasesWritten();
            _albumEditedEvent.Album.AlbumReleases.Value.Where(r => r.Monitored).Select(r => r.Id).Should().Equal(1);
        }

        [Test]
        public void should_not_change_releases_when_no_release_is_monitored()
        {
            GivenMonitored();

            Subject.UpdateAlbum(_album);

            VerifyNoReleasesWritten();
            _albumEditedEvent.Album.AlbumReleases.Value.Where(r => r.Monitored).Select(r => r.Id).Should().Equal(1);
        }

        [Test]
        public void should_not_change_releases_when_request_has_no_releases()
        {
            _album.AlbumReleases = new List<AlbumRelease>();

            Subject.UpdateAlbum(_album);

            VerifyNoReleasesWritten();
            _albumEditedEvent.Album.AlbumReleases.Value.Should().HaveCount(2);
        }

        [Test]
        public void should_publish_album_edited_with_stored_releases_and_new_monitored_release()
        {
            Subject.UpdateAlbum(_album);

            _albumEditedEvent.OldAlbum.AlbumReleases.Value.Where(r => r.Monitored).Select(r => r.Id).Should().Equal(1);
            _albumEditedEvent.Album.AlbumReleases.Value.Where(r => r.Monitored).Select(r => r.Id).Should().Equal(2);
            _albumEditedEvent.Album.AlbumReleases.Value.Single(r => r.Id == 1).ReleaseDate.Should().Be(new DateTime(1983, 1, 20));
        }

        [Test]
        public void should_publish_album_edited_before_saving_monitored_release()
        {
            var calls = new List<string>();

            Mocker.GetMock<IEventAggregator>()
                .Setup(s => s.PublishEvent(It.IsAny<AlbumEditedEvent>()))
                .Callback(() => calls.Add("album edited"));

            Mocker.GetMock<IReleaseService>()
                .Setup(s => s.SetMonitored(It.IsAny<AlbumRelease>()))
                .Callback(() => calls.Add("monitored release saved"));

            Subject.UpdateAlbum(_album);

            calls.Should().Equal("album edited", "monitored release saved");
        }
    }
}
