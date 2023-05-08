using NzbDrone.Core.Notifications;

namespace Lidarr.Api.V1.Notifications
{
    public class NotificationResource : ProviderResource<NotificationResource>
    {
        public string Link { get; set; }
        public bool OnGrab { get; set; }
        public bool OnReleaseImport { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnAlbumDelete { get; set; }
        public bool OnArtistDelete { get; set; }
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
        public bool SupportsOnAlbumDelete { get; set; }
        public bool SupportsOnArtistDelete { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool SupportsOnHealthRestored { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool SupportsOnDownloadFailure { get; set; }
        public bool SupportsOnImportFailure { get; set; }
        public bool SupportsOnTrackRetag { get; set; }
        public bool SupportsOnApplicationUpdate { get; set; }
        public string TestCommand { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            if (definition == null)
            {
                return default(NotificationResource);
            }

            var resource = base.ToResource(definition);

            resource.OnGrab = definition.OnGrab;
            resource.OnReleaseImport = definition.OnReleaseImport;
            resource.OnUpgrade = definition.OnUpgrade;
            resource.OnRename = definition.OnRename;
            resource.OnAlbumDelete = definition.OnAlbumDelete;
            resource.OnArtistDelete = definition.OnArtistDelete;
            resource.OnHealthIssue = definition.OnHealthIssue;
            resource.OnHealthRestored = definition.OnHealthRestored;
            resource.OnDownloadFailure = definition.OnDownloadFailure;
            resource.OnImportFailure = definition.OnImportFailure;
            resource.OnTrackRetag = definition.OnTrackRetag;
            resource.OnApplicationUpdate = definition.OnApplicationUpdate;
            resource.SupportsOnGrab = definition.SupportsOnGrab;
            resource.SupportsOnReleaseImport = definition.SupportsOnReleaseImport;
            resource.SupportsOnUpgrade = definition.SupportsOnUpgrade;
            resource.SupportsOnRename = definition.SupportsOnRename;
            resource.SupportsOnAlbumDelete = definition.SupportsOnAlbumDelete;
            resource.SupportsOnArtistDelete = definition.SupportsOnArtistDelete;
            resource.SupportsOnHealthIssue = definition.SupportsOnHealthIssue;
            resource.SupportsOnHealthRestored = definition.SupportsOnHealthRestored;
            resource.IncludeHealthWarnings = definition.IncludeHealthWarnings;
            resource.SupportsOnDownloadFailure = definition.SupportsOnDownloadFailure;
            resource.SupportsOnImportFailure = definition.SupportsOnImportFailure;
            resource.SupportsOnTrackRetag = definition.SupportsOnTrackRetag;
            resource.SupportsOnApplicationUpdate = definition.SupportsOnApplicationUpdate;

            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource)
        {
            if (resource == null)
            {
                return default(NotificationDefinition);
            }

            var definition = base.ToModel(resource);

            definition.OnGrab = resource.OnGrab;
            definition.OnReleaseImport = resource.OnReleaseImport;
            definition.OnUpgrade = resource.OnUpgrade;
            definition.OnRename = resource.OnRename;
            definition.OnAlbumDelete = resource.OnAlbumDelete;
            definition.OnArtistDelete = resource.OnArtistDelete;
            definition.OnHealthIssue = resource.OnHealthIssue;
            definition.OnHealthRestored = resource.OnHealthRestored;
            definition.OnDownloadFailure = resource.OnDownloadFailure;
            definition.OnImportFailure = resource.OnImportFailure;
            definition.OnTrackRetag = resource.OnTrackRetag;
            definition.OnApplicationUpdate = resource.OnApplicationUpdate;
            definition.SupportsOnGrab = resource.SupportsOnGrab;
            definition.SupportsOnReleaseImport = resource.SupportsOnReleaseImport;
            definition.SupportsOnUpgrade = resource.SupportsOnUpgrade;
            definition.SupportsOnRename = resource.SupportsOnRename;
            definition.SupportsOnAlbumDelete = resource.SupportsOnAlbumDelete;
            definition.SupportsOnArtistDelete = resource.SupportsOnArtistDelete;
            definition.SupportsOnHealthIssue = resource.SupportsOnHealthIssue;
            definition.SupportsOnHealthRestored = resource.SupportsOnHealthRestored;
            definition.IncludeHealthWarnings = resource.IncludeHealthWarnings;
            definition.SupportsOnDownloadFailure = resource.SupportsOnDownloadFailure;
            definition.SupportsOnImportFailure = resource.SupportsOnImportFailure;
            definition.SupportsOnTrackRetag = resource.SupportsOnTrackRetag;
            definition.SupportsOnApplicationUpdate = resource.SupportsOnApplicationUpdate;

            return definition;
        }
    }
}
