using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications.Webhook
{
    public abstract class WebhookBase<TSettings> : NotificationBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;

        protected WebhookBase(IConfigFileProvider configFileProvider, IConfigService configService)
        {
            _configFileProvider = configFileProvider;
            _configService = configService;
        }

        public WebhookGrabPayload BuildOnGrabPayload(GrabMessage message)
        {
            var remoteAlbum = message.RemoteAlbum;
            var quality = message.Quality;

            return new WebhookGrabPayload
            {
                EventType = WebhookEventType.Grab,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Artist = new WebhookArtist(message.Artist),
                Albums = remoteAlbum.Albums.Select(x => new WebhookAlbum(x)).ToList(),
                Release = new WebhookRelease(quality, remoteAlbum),
                DownloadClient = message.DownloadClientName,
                DownloadClientType = message.DownloadClientType,
                DownloadId = message.DownloadId
            };
        }

        public WebhookImportPayload BuildOnReleaseImportPayload(AlbumDownloadMessage message)
        {
            var trackFiles = message.TrackFiles;

            var payload = new WebhookImportPayload
            {
                EventType = WebhookEventType.Download,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Artist = new WebhookArtist(message.Artist),
                Album = new WebhookAlbum(message.Album),
                Tracks = trackFiles.SelectMany(x => x.Tracks.Value.Select(y => new WebhookTrack(y))).ToList(),
                TrackFiles = trackFiles.ConvertAll(x => new WebhookTrackFile(x)),
                IsUpgrade = message.OldFiles.Any(),
                DownloadClient = message.DownloadClientInfo?.Name,
                DownloadClientType = message.DownloadClientInfo?.Type,
                DownloadId = message.DownloadId
            };

            if (message.OldFiles.Any())
            {
                payload.DeletedFiles = message.OldFiles.ConvertAll(x => new WebhookTrackFile(x));
            }

            return payload;
        }

        public WebhookDownloadFailurePayload BuildOnDownloadFailurePayload(DownloadFailedMessage message)
        {
            return new WebhookDownloadFailurePayload
            {
                EventType = WebhookEventType.DownloadFailure,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId,
                Quality = message.Quality.Quality.Name,
                QualityVersion = message.Quality.Revision.Version,
                ReleaseTitle = message.SourceTitle
            };
        }

        public WebhookImportPayload BuildOnImportFailurePayload(AlbumDownloadMessage message)
        {
            var trackFiles = message.TrackFiles;

            var payload = new WebhookImportPayload
            {
                EventType = WebhookEventType.ImportFailure,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Artist = new WebhookArtist(message.Artist),
                Tracks = trackFiles.SelectMany(x => x.Tracks.Value.Select(y => new WebhookTrack(y))).ToList(),
                TrackFiles = trackFiles.ConvertAll(x => new WebhookTrackFile(x)),
                IsUpgrade = message.OldFiles.Any(),
                DownloadClient = message.DownloadClientInfo?.Name,
                DownloadClientType = message.DownloadClientInfo?.Type,
                DownloadId = message.DownloadId
            };

            if (message.OldFiles.Any())
            {
                payload.DeletedFiles = message.OldFiles.ConvertAll(x => new WebhookTrackFile(x));
            }

            return payload;
        }

        public WebhookRenamePayload BuildOnRenamePayload(Artist artist, List<RenamedTrackFile> renamedFiles)
        {
            return new WebhookRenamePayload
            {
                EventType = WebhookEventType.Rename,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Artist = new WebhookArtist(artist),
                RenamedTrackFiles = renamedFiles.ConvertAll(x => new WebhookRenamedTrackFile(x))
            };
        }

        public WebhookRetagPayload BuildOnTrackRetagPayload(TrackRetagMessage message)
        {
            return new WebhookRetagPayload
            {
                EventType = WebhookEventType.Retag,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Artist = new WebhookArtist(message.Artist),
                TrackFile = new WebhookTrackFile(message.TrackFile)
            };
        }

        public WebhookAlbumDeletePayload BuildOnAlbumDelete(AlbumDeleteMessage deleteMessage)
        {
            return new WebhookAlbumDeletePayload
            {
                EventType = WebhookEventType.AlbumDelete,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Album = new WebhookAlbum(deleteMessage.Album),
                DeletedFiles = deleteMessage.DeletedFiles
            };
        }

        public WebhookArtistDeletePayload BuildOnArtistDelete(ArtistDeleteMessage deleteMessage)
        {
            return new WebhookArtistDeletePayload
            {
                EventType = WebhookEventType.ArtistDelete,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Artist = new WebhookArtist(deleteMessage.Artist),
                DeletedFiles = deleteMessage.DeletedFiles
            };
        }

        protected WebhookHealthPayload BuildHealthPayload(HealthCheck.HealthCheck healthCheck)
        {
            return new WebhookHealthPayload
            {
                EventType = WebhookEventType.Health,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Level = healthCheck.Type,
                Message = healthCheck.Message,
                Type = healthCheck.Source.Name,
                WikiUrl = healthCheck.WikiUrl?.ToString()
            };
        }

        protected WebhookHealthPayload BuildHealthRestoredPayload(HealthCheck.HealthCheck healthCheck)
        {
            return new WebhookHealthPayload
            {
                EventType = WebhookEventType.HealthRestored,
                InstanceName = _configFileProvider.InstanceName,
                Level = healthCheck.Type,
                Message = healthCheck.Message,
                Type = healthCheck.Source.Name,
                WikiUrl = healthCheck.WikiUrl?.ToString()
            };
        }

        protected WebhookApplicationUpdatePayload BuildApplicationUpdatePayload(ApplicationUpdateMessage updateMessage)
        {
            return new WebhookApplicationUpdatePayload
            {
                EventType = WebhookEventType.ApplicationUpdate,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Message = updateMessage.Message,
                PreviousVersion = updateMessage.PreviousVersion.ToString(),
                NewVersion = updateMessage.NewVersion.ToString()
            };
        }

        protected WebhookPayload BuildTestPayload()
        {
            return new WebhookGrabPayload
            {
                EventType = WebhookEventType.Test,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Artist = new WebhookArtist
                {
                    Id = 1,
                    Name = "Test Name",
                    Path = "C:\\testpath",
                    MBId = "aaaaa-aaa-aaaa-aaaaaa"
                },
                Albums = new List<WebhookAlbum>
                {
                    new ()
                    {
                        Id = 123,
                        Title = "Test title"
                    }
                }
            };
        }
    }
}
