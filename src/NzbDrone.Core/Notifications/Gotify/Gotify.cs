using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Notifications.Gotify
{
    public class Gotify : NotificationBase<GotifySettings>
    {
        private readonly IGotifyProxy _proxy;
        private readonly Logger _logger;

        public Gotify(IGotifyProxy proxy, Logger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public override string Name => "Gotify";
        public override string Link => "https://gotify.net/";

        public override void OnGrab(GrabMessage grabMessage)
        {
            SendNotification(ALBUM_GRABBED_TITLE, grabMessage.Message, grabMessage.Artist);
        }

        public override void OnReleaseImport(AlbumDownloadMessage message)
        {
            SendNotification(ALBUM_DOWNLOADED_TITLE, message.Message, message.Artist);
        }

        public override void OnAlbumDelete(AlbumDeleteMessage deleteMessage)
        {
            SendNotification(ALBUM_DELETED_TITLE, deleteMessage.Message, deleteMessage.Album?.Artist);
        }

        public override void OnArtistDelete(ArtistDeleteMessage deleteMessage)
        {
            SendNotification(ARTIST_DELETED_TITLE, deleteMessage.Message, deleteMessage.Artist);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            SendNotification(HEALTH_ISSUE_TITLE, healthCheck.Message, null);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            SendNotification(HEALTH_RESTORED_TITLE, $"The following issue is now resolved: {previousCheck.Message}", null);
        }

        public override void OnDownloadFailure(DownloadFailedMessage message)
        {
            SendNotification(DOWNLOAD_FAILURE_TITLE, message.Message, null);
        }

        public override void OnImportFailure(AlbumDownloadMessage message)
        {
            SendNotification(IMPORT_FAILURE_TITLE, message.Message, message.Artist);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            SendNotification(APPLICATION_UPDATE_TITLE, updateMessage.Message, null);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var isMarkdown = false;
                const string title = "Test Notification";

                var sb = new StringBuilder();
                sb.AppendLine("This is a test message from Lidarr");

                if (Settings.IncludeArtistPoster)
                {
                    isMarkdown = true;

                    sb.AppendLine("\r![](https://raw.githubusercontent.com/Lidarr/Lidarr/develop/Logo/128.png)");
                }

                var payload = new GotifyMessage
                {
                    Title = title,
                    Message = sb.ToString(),
                    Priority = Settings.Priority
                };

                payload.SetContentType(isMarkdown);

                _proxy.SendNotification(payload, Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message");
                failures.Add(new ValidationFailure("", "Unable to send test message"));
            }

            return new ValidationResult(failures);
        }

        private void SendNotification(string title, string message, Artist artist)
        {
            var isMarkdown = false;
            var sb = new StringBuilder();

            sb.AppendLine(message);

            if (Settings.IncludeArtistPoster && artist != null)
            {
                var poster = artist.Metadata.Value.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Poster)?.Url;

                if (poster != null)
                {
                    isMarkdown = true;
                    sb.AppendLine($"\r![]({poster})");
                }
            }

            var payload = new GotifyMessage
            {
                Title = title,
                Message = sb.ToString(),
                Priority = Settings.Priority
            };

            payload.SetContentType(isMarkdown);

            _proxy.SendNotification(payload, Settings);
        }
    }
}
