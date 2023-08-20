using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles.TrackImport.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.TrackImport.Specifications
{
    [TestFixture]
    public class AlreadyImportedSpecificationFixture : CoreTest<AlreadyImportedSpecification>
    {
        private Artist _artist;
        private Album _album;
        private AlbumRelease _albumRelease;
        private Track _track;
        private LocalTrack _localTrack;
        private LocalAlbumRelease _localAlbumRelease;
        private DownloadClientItem _downloadClientItem;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\Music\30 Rock".AsOsAgnostic())
                                     .Build();

            _album = Builder<Album>.CreateNew()
                .With(x => x.Artist = _artist)
                .With(e => e.ReleaseDate = DateTime.UtcNow)
                .Build();

            _track = Builder<Track>.CreateNew()
                .With(e => e.TrackNumber = "1")
                .Build();

            _albumRelease = Builder<AlbumRelease>.CreateNew()
                .With(x => x.Album = _album)
                .With(x => x.Tracks = new List<Track> { _track })
                .Build();

            _localTrack = new LocalTrack
            {
                Album = _album,
                Artist = _artist,
                Tracks = new List<Track> { _track },
                Path = @"C:\Test\Unsorted\30 Rock\30.rock.track1.mp3".AsOsAgnostic(),
            };

            _localAlbumRelease = new LocalAlbumRelease
            {
                AlbumRelease = _albumRelease,
                LocalTracks = new List<LocalTrack> { _localTrack }
            };

            _downloadClientItem = Builder<DownloadClientItem>.CreateNew()
                .Build();
        }

        private void GivenHistory(List<EntityHistory> history)
        {
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.GetByAlbum(It.IsAny<int>(), It.IsAny<EntityHistoryEventType?>()))
                .Returns(history);
        }

        [Test]
        public void should_accepted_if_download_client_item_is_null()
        {
            Subject.IsSatisfiedBy(_localAlbumRelease, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_if_episode_does_not_have_file()
        {
            _albumRelease.Tracks.Value.ForEach(x => x.TrackFileId = 0);

            Subject.IsSatisfiedBy(_localAlbumRelease, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_if_episode_has_not_been_imported()
        {
            var history = Builder<EntityHistory>.CreateListOfSize(1)
                .All()
                .With(h => h.AlbumId = _album.Id)
                .With(h => h.EventType = EntityHistoryEventType.Grabbed)
                .Build()
                .ToList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localAlbumRelease, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_if_episode_was_grabbed_after_being_imported()
        {
            var history = Builder<EntityHistory>.CreateListOfSize(3)
                .All()
                .With(h => h.AlbumId = _album.Id)
                .TheFirst(1)
                .With(h => h.EventType = EntityHistoryEventType.Grabbed)
                .With(h => h.Date = DateTime.UtcNow)
                .TheNext(1)
                .With(h => h.EventType = EntityHistoryEventType.DownloadImported)
                .With(h => h.Date = DateTime.UtcNow.AddDays(-1))
                .TheNext(1)
                .With(h => h.EventType = EntityHistoryEventType.Grabbed)
                .With(h => h.Date = DateTime.UtcNow.AddDays(-2))
                .Build()
                .ToList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localAlbumRelease, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_if_episode_imported_after_being_grabbed()
        {
            var history = Builder<EntityHistory>.CreateListOfSize(2)
                .All()
                .With(h => h.AlbumId = _album.Id)
                .TheFirst(1)
                .With(h => h.EventType = EntityHistoryEventType.DownloadImported)
                .With(h => h.Date = DateTime.UtcNow.AddDays(-1))
                .TheNext(1)
                .With(h => h.EventType = EntityHistoryEventType.Grabbed)
                .With(h => h.Date = DateTime.UtcNow.AddDays(-2))
                .Build()
                .ToList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localAlbumRelease, _downloadClientItem).Accepted.Should().BeTrue();
        }
    }
}
