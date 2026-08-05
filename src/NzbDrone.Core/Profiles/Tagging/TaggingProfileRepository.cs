using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Profiles.Tagging
{
    public interface ITaggingProfileRepository : IBasicRepository<TaggingProfile>
    {
    }

    public class TaggingProfileRepository : BasicRepository<TaggingProfile>, ITaggingProfileRepository
    {
        public TaggingProfileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
