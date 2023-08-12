using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Update.History.Events;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<AlbumGrabbedEvent>,
          IHandle<AlbumImportedEvent>,
          IHandle<ArtistRenamedEvent>,
          IHandle<AlbumDeletedEvent>,
          IHandle<ArtistsDeletedEvent>,
          IHandle<HealthCheckFailedEvent>,
          IHandle<HealthCheckRestoredEvent>,
          IHandle<DownloadFailedEvent>,
          IHandle<AlbumImportIncompleteEvent>,
          IHandle<TrackFileRetaggedEvent>,
          IHandle<UpdateInstalledEvent>,
          IHandleAsync<RenameCompletedEvent>,
          IHandleAsync<DeleteCompletedEvent>,
          IHandleAsync<HealthCheckCompleteEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly INotificationStatusService _notificationStatusService;
        private readonly Logger _logger;

        public NotificationService(INotificationFactory notificationFactory, INotificationStatusService notificationStatusService, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _notificationStatusService = notificationStatusService;
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

        private string FormatMissing(object value)
        {
            var text = value?.ToString();
            return text.IsNullOrWhiteSpace() ? "<missing>" : text;
        }

        private string GetTrackRetagMessage(Artist artist, TrackFile trackFile, Dictionary<string, Tuple<string, string>> diff)
        {
            return string.Format("{0}:\n{1}",
                                 trackFile.Path,
                                 string.Join("\n", diff.Select(x => $"{x.Key}: {FormatMissing(x.Value.Item1)} ? {FormatMissing(x.Value.Item2)}")));
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

            // TODO: this message could be more clear
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
                RemoteAlbum = message.Album,
                DownloadClientName = message.DownloadClientName,
                DownloadClientType = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleArtist(notification.Definition, message.Album.Artist))
                    {
                        continue;
                    }

                    notification.OnGrab(grabMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Error(ex, "Unable to send OnGrab notification to {0}", notification.Definition.Name);
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
                DownloadClientInfo = message.DownloadClientInfo,
                DownloadId = message.DownloadId,
                TrackFiles = message.ImportedTracks,
                OldFiles = message.OldFiles,
            };

            foreach (var notification in _notificationFactory.OnReleaseImportEnabled())
            {
                try
                {
                    if (ShouldHandleArtist(notification.Definition, message.Artist))
                    {
                        if (downloadMessage.OldFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
                        {
                            notification.OnReleaseImport(downloadMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnReleaseImport notification to: " + notification.Definition.Name);
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
                        notification.OnRename(message.Artist, message.RenamedFiles);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnRename notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(AlbumDeletedEvent message)
        {
            var deleteMessage = new AlbumDeleteMessage(message.Album, message.DeleteFiles);

            foreach (var notification in _notificationFactory.OnAlbumDeleteEnabled())
            {
                try
                {
                    if (ShouldHandleArtist(notification.Definition, deleteMessage.Album.Artist))
                    {
                        notification.OnAlbumDelete(deleteMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnAlbumDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(ArtistsDeletedEvent message)
        {
            foreach (var artist in message.Artists)
            {
                var deleteMessage = new ArtistDeleteMessage(artist, message.DeleteFiles);

                foreach (var notification in _notificationFactory.OnArtistDeleteEnabled())
                {
                    try
                    {
                        if (ShouldHandleArtist(notification.Definition, deleteMessage.Artist))
                        {
                            notification.OnArtistDelete(deleteMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _notificationStatusService.RecordFailure(notification.Definition.Id);
                        _logger.Warn(ex, "Unable to send OnArtistDelete notification to: " + notification.Definition.Name);
                    }
                }
            }
        }

        public void Handle(HealthCheckFailedEvent message)
        {
            // Don't send health check notifications during the start up grace period,
            // once that duration expires they they'll be retested and fired off if necessary.
            if (message.IsInStartupGracePeriod)
            {
                return;
            }

            foreach (var notification in _notificationFactory.OnHealthIssueEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.HealthCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthIssue(message.HealthCheck);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnHealthIssue notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(HealthCheckRestoredEvent message)
        {
            if (message.IsInStartupGracePeriod)
            {
                return;
            }

            foreach (var notification in _notificationFactory.OnHealthRestoredEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.PreviousCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthRestored(message.PreviousCheck);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnHealthRestored notification to: " + notification.Definition.Name);
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
                SourceTitle = message.SourceTitle,
                Message = message.Message
            };

            foreach (var notification in _notificationFactory.OnDownloadFailureEnabled())
            {
                try
                {
                    if (ShouldHandleArtist(notification.Definition, message.TrackedDownload.RemoteAlbum.Artist))
                    {
                        notification.OnDownloadFailure(downloadFailedMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnDownloadFailure notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(AlbumImportIncompleteEvent message)
        {
            // TODO: Build out this message so that we can pass on what failed and what was successful
            var downloadMessage = new AlbumDownloadMessage
            {
                Message = GetAlbumIncompleteImportMessage(message.TrackedDownload.DownloadItem.Title)
            };

            foreach (var notification in _notificationFactory.OnImportFailureEnabled())
            {
                try
                {
                    if (ShouldHandleArtist(notification.Definition, message.TrackedDownload.RemoteAlbum.Artist))
                    {
                        notification.OnImportFailure(downloadMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnImportFailure notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(TrackFileRetaggedEvent message)
        {
            var retagMessage = new TrackRetagMessage
            {
                Message = GetTrackRetagMessage(message.Artist, message.TrackFile, message.Diff),
                Artist = message.Artist,
                Album = message.TrackFile.Album,
                Release = message.TrackFile.Tracks.Value.First().AlbumRelease.Value,
                TrackFile = message.TrackFile,
                Diff = message.Diff,
                Scrubbed = message.Scrubbed
            };

            foreach (var notification in _notificationFactory.OnTrackRetagEnabled())
            {
                try
                {
                    if (ShouldHandleArtist(notification.Definition, message.Artist))
                    {
                        notification.OnTrackRetag(retagMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnTrackRetag notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(UpdateInstalledEvent message)
        {
            var updateMessage = new ApplicationUpdateMessage();
            updateMessage.Message = $"Lidarr updated from {message.PreviousVerison.ToString()} to {message.NewVersion.ToString()}";
            updateMessage.PreviousVersion = message.PreviousVerison;
            updateMessage.NewVersion = message.NewVersion;

            foreach (var notification in _notificationFactory.OnApplicationUpdateEnabled())
            {
                try
                {
                    notification.OnApplicationUpdate(updateMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnApplicationUpdate notification to: " + notification.Definition.Name);
                }
            }
        }

        public void HandleAsync(RenameCompletedEvent message)
        {
            ProcessQueue();
        }

        public void HandleAsync(HealthCheckCompleteEvent message)
        {
            ProcessQueue();
        }

        public void HandleAsync(DeleteCompletedEvent message)
        {
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            foreach (var notification in _notificationFactory.GetAvailableProviders())
            {
                try
                {
                    notification.ProcessQueue();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to process notification queue for " + notification.Definition.Name);
                }
            }
        }
    }
}
