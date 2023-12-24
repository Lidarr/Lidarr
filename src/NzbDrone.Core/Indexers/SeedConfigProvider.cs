using System;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.Indexers
{
    public interface ISeedConfigProvider
    {
        TorrentSeedConfiguration GetSeedConfiguration(RemoteAlbum release);
        TorrentSeedConfiguration GetSeedConfiguration(int indexerId, bool discography);
    }

    public class SeedConfigProvider : ISeedConfigProvider, IHandle<ProviderUpdatedEvent<IIndexer>>
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly ICached<SeedCriteriaSettings> _cache;

        public SeedConfigProvider(IIndexerFactory indexerFactory, ICacheManager cacheManager)
        {
            _indexerFactory = indexerFactory;
            _cache = cacheManager.GetRollingCache<SeedCriteriaSettings>(GetType(), "criteriaByIndexer", TimeSpan.FromHours(1));
        }

        public TorrentSeedConfiguration GetSeedConfiguration(RemoteAlbum remoteAlbum)
        {
            if (remoteAlbum.Release.DownloadProtocol != nameof(TorrentDownloadProtocol))
            {
                return null;
            }

            if (remoteAlbum.Release.IndexerId == 0)
            {
                return null;
            }

            return GetSeedConfiguration(remoteAlbum.Release.IndexerId, remoteAlbum.ParsedAlbumInfo.Discography);
        }

        public TorrentSeedConfiguration GetSeedConfiguration(int indexerId, bool fullSeason)
        {
            if (indexerId == 0)
            {
                return null;
            }

            var seedCriteria = _cache.Get(indexerId.ToString(), () => FetchSeedCriteria(indexerId));

            if (seedCriteria == null)
            {
                return null;
            }

            var seedConfig = new TorrentSeedConfiguration
            {
                Ratio = seedCriteria.SeedRatio
            };

            var seedTime = fullSeason ? seedCriteria.DiscographySeedTime : seedCriteria.SeedTime;
            if (seedTime.HasValue)
            {
                seedConfig.SeedTime = TimeSpan.FromMinutes(seedTime.Value);
            }

            return seedConfig;
        }

        private SeedCriteriaSettings FetchSeedCriteria(int indexerId)
        {
            try
            {
                var indexer = _indexerFactory.Get(indexerId);
                var torrentIndexerSettings = indexer.Settings as ITorrentIndexerSettings;

                return torrentIndexerSettings?.SeedCriteria;
            }
            catch (ModelNotFoundException)
            {
                return null;
            }
        }

        public void Handle(ProviderUpdatedEvent<IIndexer> message)
        {
            _cache.Clear();
        }
    }
}
