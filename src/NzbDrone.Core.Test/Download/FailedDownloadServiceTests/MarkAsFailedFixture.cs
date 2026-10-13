using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.FailedDownloadServiceTests
{
    [TestFixture]
    public class MarkAsFailedFixture : CoreTest<FailedDownloadService>
    {
        private TrackedDownload _trackedDownload;
        private List<EntityHistory> _grabHistory;

        [SetUp]
        public void Setup()
        {
            _grabHistory = Builder<EntityHistory>.CreateListOfSize(2)
                .All()
                .With(h => h.Data = new Dictionary<string, string>())
                .BuildList();

            _trackedDownload = Builder<TrackedDownload>.CreateNew()
                .With(c => c.DownloadItem = Builder<DownloadClientItem>.CreateNew().Build())
                .Build();

            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.Find(_trackedDownload.DownloadItem.DownloadId, EntityHistoryEventType.Grabbed))
                .Returns(_grabHistory);
        }

        private bool IsExpectedEvent(DownloadFailedEvent e)
        {
            return e.Message == "Release excluded by filter" && e.TrackedDownload == _trackedDownload && !e.SkipRedownload;
        }

        [Test]
        public void should_publish_failed_event_with_custom_message()
        {
            Subject.MarkAsFailed(_trackedDownload, "Release excluded by filter");

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.Is<DownloadFailedEvent>(e => IsExpectedEvent(e))), Times.Once());
        }

        [Test]
        public void should_use_default_message_for_manual_mark()
        {
            Subject.MarkAsFailed(_trackedDownload, skipRedownload: true);

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.Is<DownloadFailedEvent>(e => e.Message == "Manually marked as failed" && e.SkipRedownload)), Times.Once());
        }

        [Test]
        public void should_not_publish_when_download_was_not_grabbed()
        {
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.Find(It.IsAny<string>(), EntityHistoryEventType.Grabbed))
                .Returns(new List<EntityHistory>());

            Subject.MarkAsFailed(_trackedDownload, "whatever");

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.IsAny<DownloadFailedEvent>()), Times.Never());
        }
    }
}
