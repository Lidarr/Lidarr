using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Music
{
    public interface IPlaylistRepository : IBasicRepository<Playlist>
    {
        Playlist GetPlaylist(string foreignPlaylistId);
    }

    public class PlaylistRepository : BasicRepository<Playlist>, IPlaylistRepository
    {
        public PlaylistRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public Playlist GetPlaylist(string foreignPlaylistId)
        {
            return Query(x => x.ForeignPlaylistId == foreignPlaylistId).FirstOrDefault();
        }
    }
}
