using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchForReleases
    {
        Task<List<DownloadDecision>> AlbumSearch(int albumId, bool missingOnly, bool userInvokedSearch, bool interactiveSearch);
        Task<List<DownloadDecision>> ArtistSearch(int artistId, bool missingOnly, bool userInvokedSearch, bool interactiveSearch);
    }

    public class ReleaseSearchService : ISearchForReleases
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly IAlbumService _albumService;
        private readonly IArtistService _artistService;
        private readonly IMakeDownloadDecision _makeDownloadDecision;
        private readonly Logger _logger;

        public ReleaseSearchService(IIndexerFactory indexerFactory,
                                IAlbumService albumService,
                                IArtistService artistService,
                                IMakeDownloadDecision makeDownloadDecision,
                                Logger logger)
        {
            _indexerFactory = indexerFactory;
            _albumService = albumService;
            _artistService = artistService;
            _makeDownloadDecision = makeDownloadDecision;
            _logger = logger;
        }

        public async Task<List<DownloadDecision>> AlbumSearch(int albumId, bool missingOnly, bool userInvokedSearch, bool interactiveSearch)
        {
            var album = _albumService.GetAlbum(albumId);

            return await AlbumSearch(album, missingOnly, userInvokedSearch, interactiveSearch);
        }

        public async Task<List<DownloadDecision>> ArtistSearch(int artistId, bool missingOnly, bool userInvokedSearch, bool interactiveSearch)
        {
            var artist = _artistService.GetArtist(artistId);

            return await ArtistSearch(artist, missingOnly, userInvokedSearch, interactiveSearch);
        }

        public async Task<List<DownloadDecision>> ArtistSearch(Artist artist, bool missingOnly, bool userInvokedSearch, bool interactiveSearch)
        {
            var downloadDecisions = new List<DownloadDecision>();

            var searchSpec = Get<ArtistSearchCriteria>(artist, userInvokedSearch, interactiveSearch);
            var albums = _albumService.GetAlbumsByArtist(artist.Id);

            albums = albums.Where(a => a.Monitored).ToList();

            searchSpec.Albums = albums;

            var decisions = await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
            downloadDecisions.AddRange(decisions);

            return DeDupeDecisions(downloadDecisions);
        }

        public async Task<List<DownloadDecision>> AlbumSearch(Album album, bool missingOnly, bool userInvokedSearch, bool interactiveSearch)
        {
            var downloadDecisions = new List<DownloadDecision>();

            var artist = _artistService.GetArtist(album.ArtistId);

            var searchSpec = Get<AlbumSearchCriteria>(artist, new List<Album> { album }, userInvokedSearch, interactiveSearch);

            searchSpec.AlbumTitle = album.Title;
            if (album.ReleaseDate.HasValue)
            {
                searchSpec.AlbumYear = album.ReleaseDate.Value.Year;
            }

            if (album.Disambiguation.IsNotNullOrWhiteSpace())
            {
                searchSpec.Disambiguation = album.Disambiguation;
            }

            var decisions = await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
            downloadDecisions.AddRange(decisions);

            return DeDupeDecisions(downloadDecisions);
        }

        private TSpec Get<TSpec>(Artist artist, List<Album> albums, bool userInvokedSearch, bool interactiveSearch)
            where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec();

            spec.Albums = albums;
            spec.Artist = artist;
            spec.UserInvokedSearch = userInvokedSearch;
            spec.InteractiveSearch = interactiveSearch;

            return spec;
        }

        private static TSpec Get<TSpec>(Artist artist, bool userInvokedSearch, bool interactiveSearch)
            where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec();
            spec.Artist = artist;
            spec.UserInvokedSearch = userInvokedSearch;
            spec.InteractiveSearch = interactiveSearch;

            return spec;
        }

        private async Task<List<DownloadDecision>> Dispatch(Func<IIndexer, Task<IList<ReleaseInfo>>> searchAction, SearchCriteriaBase criteriaBase)
        {
            var indexers = criteriaBase.InteractiveSearch ?
                _indexerFactory.InteractiveSearchEnabled() :
                _indexerFactory.AutomaticSearchEnabled();

            // Filter indexers to untagged indexers and indexers with intersecting tags
            indexers = indexers.Where(i => i.Definition.Tags.Empty() || i.Definition.Tags.Intersect(criteriaBase.Artist.Tags).Any()).ToList();

            _logger.ProgressInfo("Searching indexers for {0}. {1} active indexers", criteriaBase, indexers.Count);

            var tasks = indexers.Select(indexer => DispatchIndexer(searchAction, indexer, criteriaBase));

            var batch = await Task.WhenAll(tasks);

            var reports = batch.SelectMany(x => x).ToList();

            _logger.ProgressDebug("Total of {0} reports were found for {1} from {2} indexers", reports.Count, criteriaBase, indexers.Count);

            // Update the last search time for all albums if at least 1 indexer was searched.
            if (indexers.Any())
            {
                var lastSearchTime = DateTime.UtcNow;
                _logger.Debug("Setting last search time to: {0}", lastSearchTime);

                criteriaBase.Albums.ForEach(a => a.LastSearchTime = lastSearchTime);
                _albumService.UpdateLastSearchTime(criteriaBase.Albums);
            }

            return _makeDownloadDecision.GetSearchDecision(reports, criteriaBase).ToList();
        }

        private async Task<IList<ReleaseInfo>> DispatchIndexer(Func<IIndexer, Task<IList<ReleaseInfo>>> searchAction, IIndexer indexer, SearchCriteriaBase criteriaBase)
        {
            try
            {
                return await searchAction(indexer);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while searching for {0}", criteriaBase);
            }

            return Array.Empty<ReleaseInfo>();
        }

        private List<DownloadDecision> DeDupeDecisions(List<DownloadDecision> decisions)
        {
            // De-dupe reports by guid so duplicate results aren't returned. Pick the one with the least rejections and higher indexer priority.
            return decisions.GroupBy(d => d.RemoteAlbum.Release.Guid)
                .Select(d => d.OrderBy(v => v.Rejections.Count()).ThenBy(v => v.RemoteAlbum?.Release?.IndexerPriority ?? IndexerDefinition.DefaultPriority).First())
                .ToList();
        }
    }
}
