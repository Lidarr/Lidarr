using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists
{
    public interface IImportListRepository : IProviderRepository<ImportListDefinition>
    {
        void UpdateSettings(ImportListDefinition model);
    }

    public class ImportListRepository : ProviderRepository<ImportListDefinition>, IImportListRepository
    {
        public ImportListRepository(IMainDatabase database,
                                    IEventAggregator eventAggregator,
                                    Logger logger)
        : base(database, eventAggregator, logger)
        {
        }

        public void UpdateSettings(ImportListDefinition model)
        {
            SetFields(model, m => m.Settings);
        }
    }
}
