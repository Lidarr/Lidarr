using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotificationRepository : IProviderRepository<NotificationDefinition>
    {
        void UpdateSettings(NotificationDefinition model);
    }

    public class NotificationRepository : ProviderRepository<NotificationDefinition>, INotificationRepository
    {
        public NotificationRepository(IMainDatabase database,
                                      IEventAggregator eventAggregator,
                                      Logger logger)
        : base(database, eventAggregator, logger)
        {
        }

        public void UpdateSettings(NotificationDefinition model)
        {
            SetFields(model, m => m.Settings);
        }
    }
}
