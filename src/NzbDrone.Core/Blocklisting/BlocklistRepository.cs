using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistRepository : IBasicRepository<Blocklist>
    {
        List<Blocklist> BlocklistedByTitle(int artistId, string sourceTitle);
        List<Blocklist> BlocklistedByTorrentInfoHash(int artistId, string torrentInfoHash);
        List<Blocklist> BlocklistedByArtists(List<int> artistIds);
        void DeleteForArtists(List<int> artistIds);
    }

    public class BlocklistRepository : BasicRepository<Blocklist>, IBlocklistRepository
    {
        public BlocklistRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Blocklist> BlocklistedByTitle(int artistId, string sourceTitle)
        {
            return Query(e => e.ArtistId == artistId && e.SourceTitle.Contains(sourceTitle));
        }

        public List<Blocklist> BlocklistedByTorrentInfoHash(int artistId, string torrentInfoHash)
        {
            return Query(e => e.ArtistId == artistId && e.TorrentInfoHash.Contains(torrentInfoHash));
        }

        public List<Blocklist> BlocklistedByArtists(List<int> artistIds)
        {
            return Query(x => artistIds.Contains(x.ArtistId));
        }

        public void DeleteForArtists(List<int> artistIds)
        {
            Delete(x => artistIds.Contains(x.ArtistId));
        }

        protected override SqlBuilder PagedBuilder() => new SqlBuilder().Join<Blocklist, Artist>((b, m) => b.ArtistId == m.Id);
        protected override IEnumerable<Blocklist> PagedQuery(SqlBuilder builder) => _database.QueryJoined<Blocklist, Artist>(builder, (bl, artist) =>
                    {
                        bl.Artist = artist;
                        return bl;
                    });
    }
}
