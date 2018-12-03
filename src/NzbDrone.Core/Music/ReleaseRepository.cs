using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Music
{
    public interface IReleaseRepository : IBasicRepository<AlbumRelease>
    {
        List<AlbumRelease> FindByAlbum(int id);
        List<AlbumRelease> SetMonitored(AlbumRelease release);
    }

    public class ReleaseRepository : BasicRepository<AlbumRelease>, IReleaseRepository
    {
        public ReleaseRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<AlbumRelease> FindByAlbum(int id)
        {
            return Query.Where(r => r.AlbumId == id).ToList();
        }

        public List<AlbumRelease> SetMonitored(AlbumRelease release)
        {
            var allReleases = FindByAlbum(release.AlbumId);
            allReleases.ForEach(r => r.Monitored = r.Id == release.Id);
            Ensure.That(allReleases.Count(x => x.Monitored) == 1).IsTrue();
            UpdateMany(allReleases);
            return allReleases;
        }
    }
}
