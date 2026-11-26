using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Music;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class NotificationServiceFixture : CoreTest<NotificationService>
    {
        private Artist _artist;
        private Mock<INotification> _notification;
        private DownloadFailedEvent _downloadFailedEvent;

        [SetUp]
        public void Setup()
        {
            _artist = new Artist
            {
                Id = 12,
                Name = "Artist Name",
                Tags = new HashSet<int> { 1 }
            };

            _notification = new Mock<INotification>();
            _notification.SetupGet(n => n.Definition)
                .Returns(new NotificationDefinition { Id = 3, Name = "Notification" });

            Mocker.GetMock<INotificationFactory>()
                .Setup(s => s.OnDownloadFailureEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            Mocker.GetMock<IArtistService>()
                .Setup(s => s.GetArtist(_artist.Id))
                .Returns(_artist);

            _downloadFailedEvent = new DownloadFailedEvent
            {
                ArtistId = _artist.Id,
                AlbumIds = new List<int> { 1 },
                Quality = new QualityModel(Quality.FLAC),
                SourceTitle = "Artist Name - Album Title",
                DownloadClient = "qBittorrent",
                DownloadId = "download-id",
                Message = "Manually marked as failed"
            };
        }

        private void GivenNotificationTags(params int[] tags)
        {
            _notification.Object.Definition.Tags = new HashSet<int>(tags);
        }

        private void GivenTrackedDownload()
        {
            _downloadFailedEvent.TrackedDownload = new TrackedDownload
            {
                RemoteAlbum = new RemoteAlbum { Artist = _artist }
            };
        }

        private void VerifySent()
        {
            _notification.Verify(n => n.OnDownloadFailure(It.Is<DownloadFailedMessage>(m => m.SourceTitle == _downloadFailedEvent.SourceTitle)), Times.Once());
            Mocker.GetMock<INotificationStatusService>().Verify(s => s.RecordSuccess(3), Times.Once());
            Mocker.GetMock<INotificationStatusService>().Verify(s => s.RecordFailure(It.IsAny<int>()), Times.Never());
        }

        private void VerifyNotSent()
        {
            _notification.Verify(n => n.OnDownloadFailure(It.IsAny<DownloadFailedMessage>()), Times.Never());
            Mocker.GetMock<INotificationStatusService>().Verify(s => s.RecordFailure(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_send_download_failure_when_event_has_no_tracked_download()
        {
            Subject.Handle(_downloadFailedEvent);

            VerifySent();
        }

        [Test]
        public void should_send_download_failure_to_tagged_notification_when_event_has_no_tracked_download()
        {
            GivenNotificationTags(1);

            Subject.Handle(_downloadFailedEvent);

            VerifySent();
        }

        [Test]
        public void should_not_send_download_failure_when_artist_tags_do_not_match()
        {
            GivenNotificationTags(2);

            Subject.Handle(_downloadFailedEvent);

            VerifyNotSent();
        }

        [Test]
        public void should_use_tracked_download_artist_when_available()
        {
            GivenTrackedDownload();
            GivenNotificationTags(1);

            Subject.Handle(_downloadFailedEvent);

            VerifySent();
            Mocker.GetMock<IArtistService>().Verify(s => s.GetArtist(It.IsAny<int>()), Times.Never());
        }
    }
}
