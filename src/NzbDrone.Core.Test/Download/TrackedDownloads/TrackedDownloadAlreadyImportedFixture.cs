using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
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
    }
}
