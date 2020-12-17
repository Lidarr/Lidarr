using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class Webhook : NotificationBase<WebhookSettings>
    {
        private readonly IWebhookProxy _proxy;

        public Webhook(IWebhookProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Link => "https://wiki.servarr.com/Lidarr_Settings#Connect";

        public override void OnGrab(GrabMessage message)
        {
            var remoteAlbum = message.Album;
            var quality = message.Quality;

            var payload = new WebhookGrabPayload
            {
                EventType = WebhookEventType.Grab,
                Artist = new WebhookArtist(message.Artist),
                Albums = remoteAlbum.Albums.ConvertAll(x => new WebhookAlbum(x)
                {
                    // TODO: Stop passing these parameters inside an album v3
                    Quality = quality.Quality.Name,
                    QualityVersion = quality.Revision.Version,
                    ReleaseGroup = remoteAlbum.ParsedAlbumInfo.ReleaseGroup
                }),
                Release = new WebhookRelease(quality, remoteAlbum),
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnReleaseImport(AlbumDownloadMessage message)
        {
            var trackFiles = message.TrackFiles;

            var payload = new WebhookImportPayload
            {
                EventType = WebhookEventType.Download,
                Artist = new WebhookArtist(message.Artist),
                Tracks = trackFiles.SelectMany(x => x.Tracks.Value.Select(y => new WebhookTrack(y)
                {
                    // TODO: Stop passing these parameters inside an episode v3
                    Quality = x.Quality.Quality.Name,
                    QualityVersion = x.Quality.Revision.Version,
                    ReleaseGroup = x.ReleaseGroup
                })).ToList(),
                TrackFiles = trackFiles.ConvertAll(x => new WebhookTrackFile(x)),
                IsUpgrade = message.OldFiles.Any(),
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnRename(Artist artist)
        {
            var payload = new WebhookRenamePayload
            {
                EventType = WebhookEventType.Rename,
                Artist = new WebhookArtist(artist)
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnTrackRetag(TrackRetagMessage message)
        {
            var payload = new WebhookRetagPayload
            {
                EventType = WebhookEventType.Retag,
                Artist = new WebhookArtist(message.Artist)
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var payload = new WebhookHealthPayload
                          {
                              EventType = WebhookEventType.Health,
                              Level = healthCheck.Type,
                              Message = healthCheck.Message,
                              Type = healthCheck.Source.Name,
                              WikiUrl = healthCheck.WikiUrl?.ToString()
                          };

            _proxy.SendWebhook(payload, Settings);
        }

        public override string Name => "Webhook";

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
                var payload = new WebhookGrabPayload
                {
                    EventType = WebhookEventType.Test,
                    Artist = new WebhookArtist()
                    {
                        Id = 1,
                        Name = "Test Name",
                        Path = "C:\\testpath",
                        MBId = "aaaaa-aaa-aaaa-aaaaaa"
                    },
                    Albums = new List<WebhookAlbum>()
                    {
                            new WebhookAlbum()
                            {
                                Id = 123,
                                Title = "Test title"
                            }
                    }
                };

                _proxy.SendWebhook(payload, Settings);
            }
            catch (WebhookException ex)
            {
                return new NzbDroneValidationFailure("Url", ex.Message);
            }

            return null;
        }
    }
}
