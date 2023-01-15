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

        protected WebhookBase(IConfigFileProvider configFileProvider)
            : base()
        {
            _configFileProvider = configFileProvider;
        }

        public WebhookGrabPayload BuildOnGrabPayload(GrabMessage message)
        {
            var remoteAlbum = message.Album;
            var quality = message.Quality;

            return new WebhookGrabPayload
            {
                EventType = WebhookEventType.Grab,
                InstanceName = _configFileProvider.InstanceName,
                Artist = new WebhookArtist(message.Artist),
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

        public WebhookDownloadFailurePayload BuildOnDownloadFailurePayload(DownloadFailedMessage message)
        {
            return new WebhookDownloadFailurePayload
            {
                EventType = WebhookEventType.DownloadFailure,
                InstanceName = _configFileProvider.InstanceName,
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
        }
    }
}
