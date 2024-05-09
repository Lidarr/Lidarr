using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public class NotificationDefinition : ProviderDefinition
    {
        public bool OnGrab { get; set; }
        public bool OnReleaseImport { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnArtistAdd { get; set; }
        public bool OnArtistDelete { get; set; }
        public bool OnAlbumDelete { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool OnHealthRestored { get; set; }
        public bool OnDownloadFailure { get; set; }
        public bool OnImportFailure { get; set; }
        public bool OnTrackRetag { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool SupportsOnReleaseImport { get; set; }
        public bool SupportsOnUpgrade { get; set; }
        public bool SupportsOnRename { get; set; }
        public bool SupportsOnArtistAdd { get; set; }
        public bool SupportsOnArtistDelete { get; set; }
        public bool SupportsOnAlbumDelete { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool SupportsOnHealthRestored { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool SupportsOnDownloadFailure { get; set; }
        public bool SupportsOnImportFailure { get; set; }
        public bool SupportsOnTrackRetag { get; set; }
        public bool SupportsOnApplicationUpdate { get; set; }

        public override bool Enable => OnGrab || OnReleaseImport || (OnReleaseImport && OnUpgrade) || OnRename || OnArtistAdd || OnArtistDelete || OnAlbumDelete || OnHealthIssue || OnHealthRestored || OnDownloadFailure || OnImportFailure || OnTrackRetag || OnApplicationUpdate;
    }
}
