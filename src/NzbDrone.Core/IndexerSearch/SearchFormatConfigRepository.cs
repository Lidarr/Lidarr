using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchFormatConfigRepository : IBasicRepository<SearchFormatConfig>
    {
    }

    public class SearchFormatConfigRepository : BasicRepository<SearchFormatConfig>, ISearchFormatConfigRepository
    {
        public SearchFormatConfigRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
