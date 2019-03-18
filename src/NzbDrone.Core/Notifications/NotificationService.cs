using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<AlbumGrabbedEvent>,
          IHandle<TrackImportedEvent>,
          IHandle<AlbumImportedEvent>,
          IHandle<ArtistRenamedEvent>,
          IHandle<HealthCheckFailedEvent>,
          IHandle<DownloadFailedEvent>,
          IHandle<AlbumImportIncompleteEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly Logger _logger;

        public NotificationService(INotificationFactory notificationFactory, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _logger = logger;
        }

        private string GetMessage(Artist artist, List<Album> albums, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                qualityString += " Proper";
            }


            var albumTitles = string.Join(" + ", albums.Select(e => e.Title));

            return string.Format("{0} - {1} - [{2}]",
                                    artist.Name,
                                    albumTitles,
                                    qualityString);
        }

        private string GetTrackMessage(Artist artist, List<Track> tracks, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                qualityString += " Proper";
            }


            var trackTitles = string.Join(" + ", tracks.Select(e => e.Title));

            return string.Format("{0} - {1} - [{2}]",
                                    artist.Name,
                                    trackTitles,
                                    qualityString);
        }

        private string GetAlbumDownloadMessage(Artist artist, Album album, List<TrackFile> tracks)
        {
            return string.Format("{0} - {1} ({2} Tracks Imported)",
                artist.Name,
                album.Title,
                tracks.Count);
        }

        private string GetAlbumIncompleteImportMessage(string source)
        {
            return string.Format("Lidarr failed to Import all tracks for {0}",
                source);
        }

        private bool ShouldHandleArtist(ProviderDefinition definition, Artist artist)
        {
            if (definition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");
                return true;
            }

            if (definition.Tags.Intersect(artist.Tags).Any())
            {
                _logger.Debug("Notification and artist have one or more intersecting tags.");
                return true;
            }

            //TODO: this message could be more clear
            _logger.Debug("{0} does not have any intersecting tags with {1}. Notification will not be sent.", definition.Name, artist.Name);
            return false;
        }

        private bool ShouldHandleHealthFailure(HealthCheck.HealthCheck healthCheck, bool includeWarnings)
        {
            if (healthCheck.Type == HealthCheckResult.Error)
            {
                return true;
            }

            if (healthCheck.Type == HealthCheckResult.Warning && includeWarnings)
            {
                return true;
            }

            return false;
        }

        public void Handle(AlbumGrabbedEvent message)
        {
            var grabMessage = new GrabMessage
            {
                Message = GetMessage(message.Album.Artist, message.Album.Albums, message.Album.ParsedAlbumInfo.Quality),
                Artist = message.Album.Artist,
                Quality = message.Album.ParsedAlbumInfo.Quality,
                Album = message.Album,
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleArtist(notification.Definition, message.Album.Artist)) continue;
                    notification.OnGrab(grabMessage);
                }

                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to send OnGrab notification to {0}", notification.Definition.Name);
                }
            }
        }

        public void Handle(TrackImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadMessage = new TrackDownloadMessage

            {
                Message = GetTrackMessage(message.TrackInfo.Artist, message.TrackInfo.Tracks, message.TrackInfo.Quality),
                Artist = message.TrackInfo.Artist,
                Album = message.TrackInfo.Album,
                Release = message.TrackInfo.Release,
                TrackFile = message.ImportedTrack,
                OldFiles = message.OldFiles,
                SourcePath = message.TrackInfo.Path,
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnDownloadEnabled())
            {
                try
                {
                    if (ShouldHandleArtist(notification.Definition, message.TrackInfo.Artist))
                    {
                        if (downloadMessage.OldFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
                        {
                            notification.OnDownload(downloadMessage);
                        }
                    }
                }

                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnDownload notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(AlbumImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadMessage = new AlbumDownloadMessage

            {
                Message = GetAlbumDownloadMessage(message.Artist, message.Album, message.ImportedTracks),
                Artist = message.Artist,
                Album = message.Album,
                Release = message.AlbumRelease,
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId,
                TrackFiles = message.ImportedTracks,
                OldFiles = message.OldFiles,
            };

            foreach (var notification in _notificationFactory.OnAlbumDownloadEnabled())
            {
                try
                {
                    if (ShouldHandleArtist(notification.Definition, message.Artist))
                    {
                        notification.OnAlbumDownload(downloadMessage);
                    }
                }

                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnDownload notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(ArtistRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleArtist(notification.Definition, message.Artist))
                    {
                        notification.OnRename(message.Artist);
                    }
                }

                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnRename notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(HealthCheckFailedEvent message)
        {
            foreach (var notification in _notificationFactory.OnHealthIssueEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.HealthCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthIssue(message.HealthCheck);
                    }
                }

                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnHealthIssue notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(DownloadFailedEvent message)
        {
            var downloadFailedMessage = new DownloadFailedMessage
            {
                DownloadId = message.DownloadId,
                DownloadClient = message.DownloadClient,
                Quality = message.Quality,
                Language = message.Language,
                SourceTitle = message.SourceTitle,
                Message = message.Message
                
            };

            foreach (var notification in _notificationFactory.OnDownloadFailureEnabled())
            {
                if (ShouldHandleArtist(notification.Definition, message.TrackedDownload.RemoteAlbum.Artist))
                {
                    notification.OnDownloadFailure(downloadFailedMessage);
                }
            }
        }

        public void Handle(AlbumImportIncompleteEvent message)
        {
            // TODO: Build out this message so that we can pass on what failed and what was successful
            var downloadMessage = new AlbumDownloadMessage
            {
                Message = GetAlbumIncompleteImportMessage(message.TrackedDownload.RemoteAlbum.Release.Title),
            };

            foreach (var notification in _notificationFactory.OnDownloadFailureEnabled())
            {
                if (ShouldHandleArtist(notification.Definition, message.TrackedDownload.RemoteAlbum.Artist))
                {
                    notification.OnImportFailure(downloadMessage);
                }
            }
        }
    }
}
