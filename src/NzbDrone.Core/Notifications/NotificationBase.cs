using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public abstract class NotificationBase<TSettings> : INotification
        where TSettings : IProviderConfig, new()
    {
        protected const string ALBUM_GRABBED_TITLE = "Album Grabbed";
        protected const string ALBUM_DOWNLOADED_TITLE = "Album Downloaded";
        protected const string ALBUM_DELETED_TITLE = "Album Deleted";
        protected const string ARTIST_DELETED_TITLE = "Artist Deleted";
        protected const string HEALTH_ISSUE_TITLE = "Health Check Failure";
        protected const string DOWNLOAD_FAILURE_TITLE = "Download Failed";
        protected const string IMPORT_FAILURE_TITLE = "Import Failed";
        protected const string TRACK_RETAGGED_TITLE = "Track File Tags Updated";
        protected const string APPLICATION_UPDATE_TITLE = "Application Updated";

        protected const string ALBUM_GRABBED_TITLE_BRANDED = "Lidarr - " + ALBUM_GRABBED_TITLE;
        protected const string ALBUM_DOWNLOADED_TITLE_BRANDED = "Lidarr - " + ALBUM_DOWNLOADED_TITLE;
        protected const string ALBUM_DELETED_TITLE_BRANDED = "Lidarr - " + ALBUM_DELETED_TITLE;
        protected const string ARTIST_DELETED_TITLE_BRANDED = "Lidarr - " + ARTIST_DELETED_TITLE;
        protected const string HEALTH_ISSUE_TITLE_BRANDED = "Lidarr - " + HEALTH_ISSUE_TITLE;
        protected const string DOWNLOAD_FAILURE_TITLE_BRANDED = "Lidarr - " + DOWNLOAD_FAILURE_TITLE;
        protected const string IMPORT_FAILURE_TITLE_BRANDED = "Lidarr - " + IMPORT_FAILURE_TITLE;
        protected const string TRACK_RETAGGED_TITLE_BRANDED = "Lidarr - " + TRACK_RETAGGED_TITLE;
        protected const string APPLICATION_UPDATE_TITLE_BRANDED = "Lidarr - " + APPLICATION_UPDATE_TITLE;

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        public abstract string Link { get; }

        public virtual void OnGrab(GrabMessage grabMessage)
        {
        }

        public virtual void OnReleaseImport(AlbumDownloadMessage message)
        {
        }

        public virtual void OnRename(Artist artist, List<RenamedTrackFile> renamedFiles)
        {
        }

        public virtual void OnAlbumDelete(AlbumDeleteMessage deleteMessage)
        {
        }

        public virtual void OnArtistDelete(ArtistDeleteMessage deleteMessage)
        {
        }

        public virtual void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
        }

        public virtual void OnDownloadFailure(DownloadFailedMessage message)
        {
        }

        public virtual void OnImportFailure(AlbumDownloadMessage message)
        {
        }

        public virtual void OnTrackRetag(TrackRetagMessage message)
        {
        }

        public virtual void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
        }

        public virtual void ProcessQueue()
        {
        }

        public bool SupportsOnGrab => HasConcreteImplementation("OnGrab");
        public bool SupportsOnRename => HasConcreteImplementation("OnRename");
        public bool SupportsOnReleaseImport => HasConcreteImplementation("OnReleaseImport");
        public bool SupportsOnUpgrade => SupportsOnReleaseImport;
        public bool SupportsOnAlbumDelete => HasConcreteImplementation("OnAlbumDelete");
        public bool SupportsOnArtistDelete => HasConcreteImplementation("OnArtistDelete");
        public bool SupportsOnHealthIssue => HasConcreteImplementation("OnHealthIssue");
        public bool SupportsOnDownloadFailure => HasConcreteImplementation("OnDownloadFailure");
        public bool SupportsOnImportFailure => HasConcreteImplementation("OnImportFailure");
        public bool SupportsOnTrackRetag => HasConcreteImplementation("OnTrackRetag");
        public bool SupportsOnApplicationUpdate => HasConcreteImplementation("OnApplicationUpdate");

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        private bool HasConcreteImplementation(string methodName)
        {
            var method = GetType().GetMethod(methodName);

            if (method == null)
            {
                throw new MissingMethodException(GetType().Name, Name);
            }

            return !method.DeclaringType.IsAbstract;
        }
    }
}
