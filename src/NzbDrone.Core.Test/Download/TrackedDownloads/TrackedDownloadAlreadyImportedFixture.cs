using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.TrackedDownloads
{
    [TestFixture]
    public class TrackedDownloadAlreadyImportedFixture : CoreTest<TrackedDownloadAlreadyImported>
    {
        private List<Album> _albums;
        private TrackedDownload _trackedDownload;
        private List<EntityHistory> _historyItems;

        [SetUp]
        public void Setup()
        {
            _albums = new List<Album>();

            var remoteAlbum = Builder<RemoteAlbum>.CreateNew()
                                                      .With(r => r.Albums = _albums)
                                                      .Build();

            var downloadItem = Builder<DownloadClientItem>.CreateNew().Build();

            _trackedDownload = Builder<TrackedDownload>.CreateNew()
                                                       .With(t => t.RemoteAlbum = remoteAlbum)
                                                       .With(t => t.DownloadItem = downloadItem)
                                                       .Build();

            _historyItems = new List<EntityHistory>();
        }

        public void GivenEpisodes(int count)
        {
            _albums.AddRange(Builder<Album>.CreateListOfSize(count)
                                               .BuildList());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void should_require_every_recording_in_whole_album_history_even_when_songs_are_unmonitored(bool complete)
        {
            _trackedDownload.DownloadItem.IsFullAlbumRequest = true;
            _albums.Add(new Album
            {
                Id = 1,
                AlbumReleases = new List<AlbumRelease>
                {
                    new AlbumRelease
                    {
                        Monitored = true,
                        Tracks = new List<Track>
                        {
                            new Track { ForeignRecordingId = "first", Monitored = false },
                            new Track { ForeignRecordingId = "second", Monitored = false }
                        }
                    }
                }
            });
            _historyItems.Add(new EntityHistory
            {
                AlbumId = 1, EventType = EntityHistoryEventType.TrackFileImported,
                Data = new Dictionary<string, string> { { "RecordingId", "first" } }
            });
            if (complete)
            {
                _historyItems.Add(new EntityHistory
                {
                    AlbumId = 1, EventType = EntityHistoryEventType.TrackFileImported,
                    Data = new Dictionary<string, string> { { "RecordingId", "second" } }
                });
            }

            Subject.IsImported(_trackedDownload, _historyItems).Should().Be(complete);
        }

        public void GivenHistoryForEpisode(Album episode, params EntityHistoryEventType[] eventTypes)
        {
            foreach (var eventType in eventTypes)
            {
                _historyItems.Add(
                    Builder<EntityHistory>.CreateNew()
                                            .With(h => h.AlbumId = episode.Id)
                                            .With(h => h.EventType = eventType)
                                            .Build());
            }
        }

        [Test]
        public void should_return_false_if_there_is_no_history()
        {
            GivenEpisodes(1);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_false_if_single_episode_download_is_not_imported()
        {
            GivenEpisodes(1);

            GivenHistoryForEpisode(_albums[0], EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_false_if_no_episode_in_multi_episode_download_is_imported()
        {
            GivenEpisodes(2);

            GivenHistoryForEpisode(_albums[0], EntityHistoryEventType.Grabbed);
            GivenHistoryForEpisode(_albums[1], EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_should_return_false_if_only_one_episode_in_multi_episode_download_is_imported()
        {
            GivenEpisodes(2);

            GivenHistoryForEpisode(_albums[0], EntityHistoryEventType.DownloadImported, EntityHistoryEventType.Grabbed);
            GivenHistoryForEpisode(_albums[1], EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_true_if_single_episode_download_is_imported()
        {
            GivenEpisodes(1);

            GivenHistoryForEpisode(_albums[0], EntityHistoryEventType.DownloadImported, EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void should_return_true_if_multi_episode_download_is_imported()
        {
            GivenEpisodes(2);

            GivenHistoryForEpisode(_albums[0], EntityHistoryEventType.DownloadImported, EntityHistoryEventType.Grabbed);
            GivenHistoryForEpisode(_albums[1], EntityHistoryEventType.DownloadImported, EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeTrue();
        }

        [TestCase("target", "target", false, "RecordingId")]
        [TestCase("target", "other", false, "RecordingId")]
        [TestCase("target", "second", true, "RecordingId")]
        [TestCase("target", "target", false, "recordingId")]
        [TestCase("target", "second", true, "recordingId")]
        public void should_require_history_for_every_requested_recording(string first, string second, bool complete, string key)
        {
            _trackedDownload.RemoteAlbum.TargetRecordingIds = new List<string> { "target", "second" };
            foreach (var recording in new[] { first, second })
            {
                _historyItems.Add(new EntityHistory
                {
                    EventType = EntityHistoryEventType.TrackFileImported,
                    Data = new Dictionary<string, string> { { key, recording } }
                });
            }

            Subject.IsImported(_trackedDownload, _historyItems).Should().Be(complete);
        }

        [Test]
        public void should_read_recordings_from_tracks_for_older_import_history()
        {
            _trackedDownload.RemoteAlbum.TargetRecordingIds = new List<string> { "target" };
            _historyItems.Add(new EntityHistory { TrackId = 123, EventType = EntityHistoryEventType.TrackFileImported });
            Mocker.GetMock<ITrackService>().Setup(service => service.GetTracks(It.IsAny<IEnumerable<int>>()))
                .Returns(new List<Track> { new Track { Id = 123, ForeignRecordingId = "target" } });

            Subject.IsImported(_trackedDownload, _historyItems).Should().BeTrue();
        }

        [Test]
        public void album_import_history_alone_should_not_complete_a_targeted_download()
        {
            _trackedDownload.RemoteAlbum.TargetRecordingIds = new List<string> { "target", "second" };
            _historyItems.Add(new EntityHistory { EventType = EntityHistoryEventType.DownloadImported });

            Subject.IsImported(_trackedDownload, _historyItems).Should().BeFalse();
        }
    }
}
