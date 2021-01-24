using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download.Pending
{
    public interface IPendingReleaseRepository : IBasicRepository<PendingRelease>
    {
        void DeleteByArtistIds(List<int> artistIds);
        List<PendingRelease> AllByArtistId(int artistId);
        List<PendingRelease> WithoutFallback();
    }

    public class PendingReleaseRepository : BasicRepository<PendingRelease>, IPendingReleaseRepository
    {
        public PendingReleaseRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteByArtistIds(List<int> artistIds)
        {
            Delete(x => artistIds.Contains(x.ArtistId));
        }

        public List<PendingRelease> AllByArtistId(int artistId)
        {
            return Query(p => p.ArtistId == artistId);
        }

        public List<PendingRelease> WithoutFallback()
        {
            return Query(p => p.Reason != PendingReleaseReason.Fallback);
        }
    }
}
