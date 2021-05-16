using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Music
{
    public interface IPlaylistEntryRepository : IBasicRepository<PlaylistEntry>
    {
        List<PlaylistEntry> UpsertMany(List<PlaylistEntry> entries);
        List<PlaylistEntry> FindByPlaylistId(int playlistId);
        List<int> FindPlaylistsByForeignAlbumId(string foreignAlbumId);
    }

    public class PlaylistEntryRepository : BasicRepository<PlaylistEntry>, IPlaylistEntryRepository
    {
        public PlaylistEntryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<PlaylistEntry> UpsertMany(List<PlaylistEntry> entries)
        {
            var playlistIds = entries.Select(x => x.PlaylistId).Distinct();

            Delete(x => playlistIds.Contains(x.PlaylistId));

            InsertMany(entries);

            return entries;
        }

        public List<PlaylistEntry> FindByPlaylistId(int playlistId)
        {
            return Query(x => x.PlaylistId == playlistId).ToList();
        }

        public List<PlaylistEntry> FindByPlaylistId(List<int> playlistIds)
        {
            return Query(x => playlistIds.Contains(x.Id)).ToList();
        }

        public List<int> FindPlaylistsByForeignAlbumId(string foreignAlbumId)
        {
            return Query(x => x.ForeignAlbumId == foreignAlbumId).Select(x => x.PlaylistId).Distinct().ToList();
        }
    }
}
