using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class NotificationBaseFixture : TestBase
    {
        private class TestSetting : IProviderConfig
        {
            public NzbDroneValidationResult Validate()
            {
                return new NzbDroneValidationResult();
            }
        }

        private class TestNotificationWithOnReleaseImport : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }

            public override void OnReleaseImport(AlbumDownloadMessage message)
            {
                TestLogger.Info("OnDownload was called");
            }
        }

        private class TestNotificationWithAllEvents : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }

            public override void OnGrab(GrabMessage grabMessage)
            {
                TestLogger.Info("OnGrab was called");
            }

            public override void OnReleaseImport(AlbumDownloadMessage message)
            {
                TestLogger.Info("OnAlbumDownload was called");
            }

            public override void OnRename(Artist artist, List<RenamedTrackFile> renamedFiles)
            {
                TestLogger.Info("OnRename was called");
            }

            public override void OnAlbumDelete(AlbumDeleteMessage deleteMessage)
            {
                TestLogger.Info("Album OnDelete was called");
            }

            public override void OnArtistDelete(ArtistDeleteMessage deleteMessage)
            {
                TestLogger.Info("Artist OnDelete was called");
            }

            public override void OnHealthIssue(NzbDrone.Core.HealthCheck.HealthCheck artist)
            {
                TestLogger.Info("OnHealthIssue was called");
            }

            public override void OnHealthRestored(Core.HealthCheck.HealthCheck healthCheck)
            {
                TestLogger.Info("OnHealthRestored was called");
            }

            public override void OnDownloadFailure(DownloadFailedMessage message)
            {
                TestLogger.Info("OnDownloadFailure was called");
            }

            public override void OnImportFailure(AlbumDownloadMessage message)
            {
                TestLogger.Info("OnImportFailure was called");
            }

            public override void OnTrackRetag(TrackRetagMessage message)
            {
                TestLogger.Info("OnTrackRetag was called");
            }

            public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
            {
                TestLogger.Info("OnApplicationUpdate was called");
            }
        }

        private class TestNotificationWithNoEvents : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void should_support_OnUpgrade_should_link_to_OnReleaseImport()
        {
            var notification = new TestNotificationWithOnReleaseImport();

            notification.SupportsOnReleaseImport.Should().BeTrue();
            notification.SupportsOnUpgrade.Should().BeTrue();

            notification.SupportsOnGrab.Should().BeFalse();
            notification.SupportsOnRename.Should().BeFalse();
        }

        [Test]
        public void should_support_all_if_implemented()
        {
            var notification = new TestNotificationWithAllEvents();

            notification.SupportsOnGrab.Should().BeTrue();
            notification.SupportsOnReleaseImport.Should().BeTrue();
            notification.SupportsOnUpgrade.Should().BeTrue();
            notification.SupportsOnRename.Should().BeTrue();
            notification.SupportsOnHealthIssue.Should().BeTrue();
            notification.SupportsOnHealthRestored.Should().BeTrue();
            notification.SupportsOnDownloadFailure.Should().BeTrue();
            notification.SupportsOnImportFailure.Should().BeTrue();
            notification.SupportsOnTrackRetag.Should().BeTrue();
            notification.SupportsOnApplicationUpdate.Should().BeTrue();
            notification.SupportsOnAlbumDelete.Should().BeTrue();
            notification.SupportsOnArtistDelete.Should().BeTrue();
        }

        [Test]
        public void should_support_none_if_none_are_implemented()
        {
            var notification = new TestNotificationWithNoEvents();

            notification.SupportsOnGrab.Should().BeFalse();
            notification.SupportsOnReleaseImport.Should().BeFalse();
            notification.SupportsOnUpgrade.Should().BeFalse();
            notification.SupportsOnRename.Should().BeFalse();
            notification.SupportsOnHealthIssue.Should().BeFalse();
            notification.SupportsOnHealthRestored.Should().BeFalse();
            notification.SupportsOnDownloadFailure.Should().BeFalse();
            notification.SupportsOnImportFailure.Should().BeFalse();
            notification.SupportsOnTrackRetag.Should().BeFalse();
            notification.SupportsOnApplicationUpdate.Should().BeFalse();
            notification.SupportsOnAlbumDelete.Should().BeFalse();
            notification.SupportsOnArtistDelete.Should().BeFalse();
        }
    }
}
