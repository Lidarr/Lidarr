﻿using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using Marr.Data.QGen;

namespace NzbDrone.Core.Music
{
    public interface IArtistRepository : IBasicRepository<Artist>
    {
        bool ArtistPathExists(string path);
        Artist FindByName(string cleanTitle);
        Artist FindById(int dbId);
        Artist FindById(string spotifyId);
        Artist GetArtistByMetadataId(int artistMetadataId);
    }

    public class ArtistRepository : BasicRepository<Artist>, IArtistRepository
    {
        public ArtistRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        // Always explicitly join with ArtistMetadata to populate Metadata without repeated LazyLoading
        protected override QueryBuilder<Artist> Query => DataMapper.Query<Artist>().Join<Artist, ArtistMetadata>(JoinType.Inner, a => a.Metadata, (l, r) => l.ArtistMetadataId == r.Id);

        public bool ArtistPathExists(string path)
        {
            return Query.Where(c => c.Path == path).Any();
        }

        public Artist FindById(string foreignArtistId)
        {
            return Query.Where<ArtistMetadata>(m => m.ForeignArtistId == foreignArtistId).SingleOrDefault();
        }

        public Artist FindById(int dbId)
        {
            return Query.Where(s => s.Id == dbId).SingleOrDefault();
        }

        public Artist FindByName(string cleanName)
        {
            cleanName = cleanName.ToLowerInvariant();

            return Query.Where(s => s.CleanName == cleanName)
                        .SingleOrDefault();
        }

        public Artist GetArtistByMetadataId(int artistMetadataId)
        {
            return Query.Where(s => s.ArtistMetadataId == artistMetadataId).SingleOrDefault();
        }
    }
}
