using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Notifications.Webhook;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Notifiarr
{
    public class Notifiarr : WebhookBase<NotifiarrSettings>
    {
        private readonly INotifiarrProxy _proxy;

        public Notifiarr(INotifiarrProxy proxy, IConfigFileProvider configFileProvider, IConfigService configService, ITagRepository tagRepository, IMapCoversToLocal mediaCoverService)
            : base(configFileProvider, configService, tagRepository, mediaCoverService)
        {
            _proxy = proxy;
        }

        public override string Link => "https://notifiarr.com";
        public override string Name => "Notifiarr";

        public override void OnGrab(GrabMessage message)
        {
            _proxy.SendNotification(BuildOnGrabPayload(message), Settings);
        }

        public override void OnReleaseImport(AlbumDownloadMessage message)
        {
            _proxy.SendNotification(BuildOnReleaseImportPayload(message), Settings);
        }

        public override void OnDownloadFailure(DownloadFailedMessage message)
        {
            _proxy.SendNotification(BuildOnDownloadFailurePayload(message), Settings);
        }

        public override void OnImportFailure(AlbumDownloadMessage message)
        {
            _proxy.SendNotification(BuildOnImportFailurePayload(message), Settings);
        }

        public override void OnRename(Artist artist, List<RenamedTrackFile> renamedFiles)
        {
            _proxy.SendNotification(BuildOnRenamePayload(artist, renamedFiles), Settings);
        }

        public override void OnTrackRetag(TrackRetagMessage message)
        {
            _proxy.SendNotification(BuildOnTrackRetagPayload(message), Settings);
        }

        public override void OnArtistAdd(ArtistAddMessage message)
        {
            _proxy.SendNotification(BuildOnArtistAdd(message), Settings);
        }

        public override void OnArtistDelete(ArtistDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(BuildOnArtistDelete(deleteMessage), Settings);
        }

        public override void OnAlbumDelete(AlbumDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(BuildOnAlbumDelete(deleteMessage), Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(BuildHealthPayload(healthCheck), Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            _proxy.SendNotification(BuildHealthRestoredPayload(previousCheck), Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(BuildApplicationUpdatePayload(updateMessage), Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(SendWebhookTest());

            return new ValidationResult(failures);
        }

        private ValidationFailure SendWebhookTest()
        {
            try
            {
                _proxy.SendNotification(BuildTestPayload(), Settings);
            }
            catch (NotifiarrException ex)
            {
                return new NzbDroneValidationFailure("APIKey", ex.Message);
            }

            return null;
        }
    }
}
