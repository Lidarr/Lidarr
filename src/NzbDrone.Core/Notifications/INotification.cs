using System.Collections.Generic;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotification : IProvider
    {
        string Link { get; }

        void OnGrab(GrabMessage grabMessage);
        void OnReleaseImport(AlbumDownloadMessage message);
        void OnRename(Artist artist, List<RenamedTrackFile> renamedFiles);
        void OnArtistAdd(ArtistAddMessage message);
        void OnArtistDelete(ArtistDeleteMessage deleteMessage);
        void OnAlbumDelete(AlbumDeleteMessage deleteMessage);
        void OnHealthIssue(HealthCheck.HealthCheck healthCheck);
        void OnHealthRestored(HealthCheck.HealthCheck previousCheck);
        void OnApplicationUpdate(ApplicationUpdateMessage updateMessage);
        void OnDownloadFailure(DownloadFailedMessage message);
        void OnImportFailure(AlbumDownloadMessage message);
        void OnTrackRetag(TrackRetagMessage message);
        void ProcessQueue();
        bool SupportsOnGrab { get; }
        bool SupportsOnReleaseImport { get; }
        bool SupportsOnUpgrade { get; }
        bool SupportsOnRename { get; }
        bool SupportsOnArtistAdd { get; }
        bool SupportsOnArtistDelete { get; }
        bool SupportsOnAlbumDelete { get; }
        bool SupportsOnHealthIssue { get; }
        bool SupportsOnHealthRestored { get; }
        bool SupportsOnApplicationUpdate { get; }
        bool SupportsOnDownloadFailure { get; }
        bool SupportsOnImportFailure { get; }
        bool SupportsOnTrackRetag { get; }
    }
}
