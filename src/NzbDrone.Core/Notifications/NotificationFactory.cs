using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Composition;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotificationFactory : IProviderFactory<INotification, NotificationDefinition>
    {
        List<INotification> OnGrabEnabled();
        List<INotification> OnReleaseImportEnabled();
        List<INotification> OnUpgradeEnabled();
        List<INotification> OnRenameEnabled();
        List<INotification> OnAlbumDeleteEnabled();
        List<INotification> OnArtistDeleteEnabled();
        List<INotification> OnHealthIssueEnabled();
        List<INotification> OnHealthRestoredEnabled();
        List<INotification> OnDownloadFailureEnabled();
        List<INotification> OnImportFailureEnabled();
        List<INotification> OnTrackRetagEnabled();
        List<INotification> OnApplicationUpdateEnabled();
    }

    public class NotificationFactory : ProviderFactory<INotification, NotificationDefinition>, INotificationFactory
    {
        public NotificationFactory(INotificationRepository providerRepository, IEnumerable<INotification> providers, IServiceProvider container, IEventAggregator eventAggregator, Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
        }

        public List<INotification> OnGrabEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnGrab).ToList();
        }

        public List<INotification> OnReleaseImportEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnReleaseImport).ToList();
        }

        public List<INotification> OnUpgradeEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnUpgrade).ToList();
        }

        public List<INotification> OnRenameEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnRename).ToList();
        }

        public List<INotification> OnAlbumDeleteEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnAlbumDelete).ToList();
        }

        public List<INotification> OnArtistDeleteEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnArtistDelete).ToList();
        }

        public List<INotification> OnHealthIssueEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthIssue).ToList();
        }

        public List<INotification> OnHealthRestoredEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthRestored).ToList();
        }

        public List<INotification> OnDownloadFailureEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnDownloadFailure).ToList();
        }

        public List<INotification> OnImportFailureEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnImportFailure).ToList();
        }

        public List<INotification> OnTrackRetagEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnTrackRetag).ToList();
        }

        public List<INotification> OnApplicationUpdateEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnApplicationUpdate).ToList();
        }

        public override void SetProviderCharacteristics(INotification provider, NotificationDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);

            definition.SupportsOnGrab = provider.SupportsOnGrab;
            definition.SupportsOnReleaseImport = provider.SupportsOnReleaseImport;
            definition.SupportsOnUpgrade = provider.SupportsOnUpgrade;
            definition.SupportsOnRename = provider.SupportsOnRename;
            definition.SupportsOnAlbumDelete = provider.SupportsOnAlbumDelete;
            definition.SupportsOnArtistDelete = provider.SupportsOnArtistDelete;
            definition.SupportsOnHealthIssue = provider.SupportsOnHealthIssue;
            definition.SupportsOnHealthRestored = provider.SupportsOnHealthRestored;
            definition.SupportsOnDownloadFailure = provider.SupportsOnDownloadFailure;
            definition.SupportsOnImportFailure = provider.SupportsOnImportFailure;
            definition.SupportsOnTrackRetag = provider.SupportsOnTrackRetag;
            definition.SupportsOnApplicationUpdate = provider.SupportsOnApplicationUpdate;
        }
    }
}
