using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Music
{
    public interface IArtistRepository : IBasicRepository<Artist>
    {
        bool ArtistPathExists(string path);
        Artist FindByName(string cleanName);
        Artist FindById(string foreignArtistId);
        Dictionary<int, string> AllArtistPaths();
        Dictionary<int, List<int>> AllArtistsTags();
        Artist GetArtistByMetadataId(int artistMetadataId);
        List<Artist> GetArtistByMetadataId(IEnumerable<int> artistMetadataId);
    }

    public class ArtistRepository : BasicRepository<Artist>, IArtistRepository
    {
        public ArtistRepository(IMainDatabase database,
                                IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override SqlBuilder Builder() => new SqlBuilder(_database.DatabaseType)
            .Join<Artist, ArtistMetadata>((a, m) => a.ArtistMetadataId == m.Id);

        protected override List<Artist> Query(SqlBuilder builder) => Query(_database, builder).ToList();

        public static IEnumerable<Artist> Query(IDatabase database, SqlBuilder builder)
        {
            return database.QueryJoined<Artist, ArtistMetadata>(builder, (artist, metadata) =>
                    {
                        artist.Metadata = metadata;
                        return artist;
                    });
        }

        public bool ArtistPathExists(string path)
        {
            return Query(c => c.Path == path).Any();
        }

        public Artist FindById(string foreignArtistId)
        {
            var artist = Query(Builder().Where<ArtistMetadata>(m => m.ForeignArtistId == foreignArtistId)).SingleOrDefault();

            if (artist == null && foreignArtistId.IsNotNullOrWhiteSpace())
            {
                artist = Query(Builder().Where<ArtistMetadata>(x => x.OldForeignArtistIds.Contains(foreignArtistId))).SingleOrDefault();
            }

            return artist;
        }

        public Artist FindByName(string cleanName)
        {
            cleanName = cleanName.ToLowerInvariant();

            var artists = Query(s => s.CleanName == cleanName).ToList();

            return ReturnSingleArtistOrThrow(artists);
        }

        public Artist GetArtistByMetadataId(int artistMetadataId)
        {
            return Query(s => s.ArtistMetadataId == artistMetadataId).SingleOrDefault();
        }

        public Dictionary<int, string> AllArtistPaths()
        {
            using (var conn = _database.OpenConnection())
            {
                var strSql = "SELECT \"Id\" AS \"Key\", \"Path\" AS \"Value\" FROM \"Artists\"";
                return conn.Query<KeyValuePair<int, string>>(strSql).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public Dictionary<int, List<int>> AllArtistsTags()
        {
            using (var conn = _database.OpenConnection())
            {
                var strSql = "SELECT \"Id\" AS \"Key\", \"Tags\" AS \"Value\" FROM \"Artists\" WHERE \"Tags\" IS NOT NULL";
                return conn.Query<KeyValuePair<int, List<int>>>(strSql).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public List<Artist> GetArtistByMetadataId(IEnumerable<int> artistMetadataIds)
        {
            return Query(s => artistMetadataIds.Contains(s.ArtistMetadataId));
        }

        private static Artist ReturnSingleArtistOrThrow(List<Artist> artists)
        {
            if (artists.Count == 0)
            {
                return null;
            }

            if (artists.Count == 1)
            {
                return artists[0];
            }

            throw new MultipleArtistsFoundException("Expected one artist, but found {0}. Matching artists: {1}", artists.Count, string.Join(",", artists.Select(s => s.Name)));
        }
    }
}
