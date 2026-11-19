using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Music
{
    public interface IAlbumRefreshedService
    {
        void Search(Artist artist);
    }

    public class AlbumRefreshedService : IHandle<AlbumInfoRefreshedEvent>, IAlbumRefreshedService
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IAlbumService _albumService;
        private readonly Logger _logger;
        private readonly ICached<List<int>> _searchCache;

        public AlbumRefreshedService(ICacheManager cacheManager,
                                   IManageCommandQueue commandQueueManager,
                                   IAlbumService albumService,
                                   Logger logger)
        {
            _commandQueueManager = commandQueueManager;
            _albumService = albumService;
            _logger = logger;
            _searchCache = cacheManager.GetCache<List<int>>(GetType());
        }

        public void Search(Artist artist)
        {
            var allAlbums = _albumService.GetAlbumsByArtist(artist.Id);
            var toSearch = allAlbums.Where(x => x.AddOptions.SearchForNewAlbum).ToList();

            if (toSearch.Any())
            {
                _logger.Debug("Found {0} albums by artist {1} flagged for search on add", toSearch.Count, artist);
                toSearch.ForEach(x => x.AddOptions.SearchForNewAlbum = false);

                _albumService.SetAddOptions(toSearch);
            }

            var recentlyAddedIds = _searchCache.Find(artist.Id.ToString());
            if (recentlyAddedIds != null)
            {
                _logger.Debug("Found and monitored {0} albums by artist {1} during metadata refresh.", recentlyAddedIds.Count, artist);
                toSearch.AddRange(allAlbums.Where(x => recentlyAddedIds.Contains(x.Id)));
            }

            if (toSearch.Any())
            {
                _logger.Info("Searching for {0} monitored albums for artist {1} added during metadata refresh.", toSearch.Count, artist);
                _commandQueueManager.Push(new AlbumSearchCommand(toSearch.Select(e => e.Id).ToList()));
            }

            _searchCache.Remove(artist.Id.ToString());
        }

        public void Handle(AlbumInfoRefreshedEvent message)
        {
            if (message.Artist.AddOptions == null)
            {
                if (!message.Artist.Monitored)
                {
                    _logger.Debug("Artist is not monitored");
                    return;
                }

                if (message.Added.Empty())
                {
                    _logger.Debug("No new albums, skipping search");
                    return;
                }

                if (message.Added.None(a => a.ReleaseDate.HasValue))
                {
                    _logger.Debug("No new albums have an release date");
                    return;
                }

                var previouslyReleased = message.Added.Where(a =>
                        a.ReleaseDate.HasValue &&
                        a.ReleaseDate.Value.Between(DateTime.UtcNow.AddDays(-180), DateTime.UtcNow.AddDays(1)) &&
                        a.Monitored)
                    .Select(a => a.Id)
                    .ToList();

                if (previouslyReleased.Empty())
                {
                    _logger.Debug("Newly added albums all release in the future");
                    return;
                }

                _searchCache.Set(message.Artist.Id.ToString(), previouslyReleased);
            }
        }
    }
}
