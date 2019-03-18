using NzbDrone.Core.Notifications;

namespace Lidarr.Api.V1.Notifications
{
    public class NotificationResource : ProviderResource
    {
        public string Link { get; set; }
        public bool OnGrab { get; set; }
        public bool OnDownload { get; set; }
        public bool OnAlbumDownload { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool OnDownloadFailure { get; set; }
        public bool OnImportFailure { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool SupportsOnDownload { get; set; }
        public bool SupportsOnAlbumDownload { get; set; }
        public bool SupportsOnUpgrade { get; set; }
        public bool SupportsOnRename { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool SupportsOnDownloadFailure { get; set; }
        public bool SupportsOnImportFailure { get; set; }
        public string TestCommand { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            if (definition == null) return default(NotificationResource);

            var resource = base.ToResource(definition);

            resource.OnGrab = definition.OnGrab;
            resource.OnDownload = definition.OnDownload;
            resource.OnAlbumDownload = definition.OnAlbumDownload;
            resource.OnUpgrade = definition.OnUpgrade;
            resource.OnRename = definition.OnRename;
            resource.OnHealthIssue = definition.OnHealthIssue;
            resource.OnDownloadFailure = definition.OnDownloadFailure;
            resource.OnImportFailure = definition.OnImportFailure;
            resource.SupportsOnGrab = definition.SupportsOnGrab;
            resource.SupportsOnDownload = definition.SupportsOnDownload;
            resource.SupportsOnAlbumDownload = definition.SupportsOnAlbumDownload;
            resource.SupportsOnUpgrade = definition.SupportsOnUpgrade;
            resource.SupportsOnRename = definition.SupportsOnRename;
            resource.SupportsOnHealthIssue = definition.SupportsOnHealthIssue;
            resource.IncludeHealthWarnings = definition.IncludeHealthWarnings;
            resource.SupportsOnDownloadFailure = definition.SupportsOnDownloadFailure;
            resource.SupportsOnImportFailure = definition.SupportsOnImportFailure;

            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource)
        {
            if (resource == null) return default(NotificationDefinition);

            var definition = base.ToModel(resource);

            definition.OnGrab = resource.OnGrab;
            definition.OnDownload = resource.OnDownload;
            definition.OnAlbumDownload = resource.OnAlbumDownload;
            definition.OnUpgrade = resource.OnUpgrade;
            definition.OnRename = resource.OnRename;
            definition.OnHealthIssue = resource.OnHealthIssue;
            definition.OnDownloadFailure = resource.OnDownloadFailure;
            definition.OnImportFailure = resource.OnImportFailure;
            definition.SupportsOnGrab = resource.SupportsOnGrab;
            definition.SupportsOnDownload = resource.SupportsOnDownload;
            definition.SupportsOnAlbumDownload = resource.SupportsOnAlbumDownload;
            definition.SupportsOnUpgrade = resource.SupportsOnUpgrade;
            definition.SupportsOnRename = resource.SupportsOnRename;
            definition.SupportsOnHealthIssue = resource.SupportsOnHealthIssue;
            definition.IncludeHealthWarnings = resource.IncludeHealthWarnings;
            definition.SupportsOnDownloadFailure = resource.SupportsOnDownloadFailure;
            definition.SupportsOnImportFailure = resource.SupportsOnImportFailure;

            return definition;
        }
    }
}
