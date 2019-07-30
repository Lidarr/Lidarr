using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class NewznabRequestGenerator : IIndexerRequestGenerator
    {
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;
        public int MaxPages { get; set; }
        public int PageSize { get; set; }
        public NewznabSettings Settings { get; set; }

        public NewznabRequestGenerator(INewznabCapabilitiesProvider capabilitiesProvider)
        {
            _capabilitiesProvider = capabilitiesProvider;

            MaxPages = 30;
            PageSize = 100;
        }

        private bool SupportsSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportedSearchParameters != null &&
                       capabilities.SupportedSearchParameters.Contains("q");
            }
        }

        private bool SupportsAudioSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportedAudioSearchParameters != null &&
                       capabilities.SupportedAudioSearchParameters.Contains("q") &&
                       capabilities.SupportedAudioSearchParameters.Contains("artist") &&
                       capabilities.SupportedAudioSearchParameters.Contains("album");
            }
        }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

            if (capabilities.SupportedAudioSearchParameters != null)
            {
                pageableRequests.Add(GetPagedRequests(MaxPages, Settings.Categories, "music", ""));
            }
            else if (capabilities.SupportedSearchParameters != null)
            {
                pageableRequests.Add(GetPagedRequests(MaxPages, Settings.Categories, "search", ""));
            }

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (SupportsAudioSearch)
            {
                AddAlbumRequests(pageableRequests, searchCriteria, "&artist={0}&album={1}", AddAudioPageableRequests);
            }

            if (SupportsSearch)
            {
                AddAlbumRequests(pageableRequests, searchCriteria, "&q={0}+{1}", AddSearchPageableRequests);
            }

            return pageableRequests;
        }

        private void AddAlbumRequests(IndexerPageableRequestChain pageableRequests, AlbumSearchCriteria searchCriteria, string paramFormat, Action<IndexerPageableRequestChain, SearchCriteriaBase, string> AddRequests)
        {
            var albumQuery = searchCriteria.AlbumQueries[0];
            var artistQuery = searchCriteria.ArtistQueries[0];

            // search using standard name
            pageableRequests.AddTier();
            AddRequests(pageableRequests, searchCriteria, NewsnabifyTitle(string.Format(paramFormat, artistQuery, albumQuery)));

            // using artist alias
            pageableRequests.AddTier();
            foreach (var artistAlt in searchCriteria.ArtistQueries.Skip(1))
            {
                AddRequests(pageableRequests, searchCriteria, NewsnabifyTitle(string.Format(paramFormat, artistAlt, albumQuery)));
            }

            // using album alias
            pageableRequests.AddTier();
            foreach (var albumAlt in searchCriteria.AlbumQueries.Skip(1))
            {
                AddRequests(pageableRequests, searchCriteria, NewsnabifyTitle(string.Format(paramFormat, artistQuery, albumAlt)));
            }

            // using aliases for both
            foreach (var artistAlt in searchCriteria.ArtistQueries.Skip(1))
            {
                foreach (var albumAlt in searchCriteria.AlbumQueries.Skip(1))
                {
                    AddRequests(pageableRequests, searchCriteria, NewsnabifyTitle(string.Format(paramFormat, artistAlt, albumAlt)));
                }
            }
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (SupportsAudioSearch)
            {
                AddArtistRequests(pageableRequests, searchCriteria, "&artist={0}", AddAudioPageableRequests);
            }

            if (SupportsSearch)
            {
                AddArtistRequests(pageableRequests, searchCriteria, "&q={0}", AddSearchPageableRequests);
            }

            return pageableRequests;
        }

        private void AddArtistRequests(IndexerPageableRequestChain pageableRequests, SearchCriteriaBase searchCriteria, string paramFormat, Action<IndexerPageableRequestChain, SearchCriteriaBase, string> AddRequests)
        {
            var artistQuery = searchCriteria.ArtistQueries[0];

            // search using standard name
            pageableRequests.AddTier();
            AddRequests(pageableRequests, searchCriteria, NewsnabifyTitle(string.Format(paramFormat, artistQuery)));

            // using artist alias
            pageableRequests.AddTier();
            foreach (var artistAlt in searchCriteria.ArtistQueries.Skip(1))
            {
                AddRequests(pageableRequests, searchCriteria, NewsnabifyTitle(string.Format(paramFormat, artistAlt)));
            }
        }

        private void AddAudioPageableRequests(IndexerPageableRequestChain chain, SearchCriteriaBase searchCriteria, string parameters)
        {
            chain.AddTier();

            chain.Add(GetPagedRequests(MaxPages, Settings.Categories, "music", $"&q={parameters}"));
        }

        private void AddSearchPageableRequests(IndexerPageableRequestChain chain, SearchCriteriaBase searchCriteria, string parameters)
        {
            chain.Add(GetPagedRequests(MaxPages, Settings.Categories, "search", parameters));
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(int maxPages, IEnumerable<int> categories, string searchType, string parameters)
        {
            if (categories.Empty())
            {
                yield break;
            }

            var categoriesQuery = string.Join(",", categories.Distinct());

            var baseUrl =
                $"{Settings.BaseUrl.TrimEnd('/')}{Settings.ApiPath.TrimEnd('/')}?t={searchType}&cat={categoriesQuery}&extended=1{Settings.AdditionalParameters}";

            if (Settings.ApiKey.IsNotNullOrWhiteSpace())
            {
                baseUrl += "&apikey=" + Settings.ApiKey;
            }

            if (PageSize == 0)
            {
                yield return new IndexerRequest($"{baseUrl}{parameters}", HttpAccept.Rss);
            }
            else
            {
                for (var page = 0; page < maxPages; page++)
                {
                    yield return new IndexerRequest($"{baseUrl}&offset={page * PageSize}&limit={PageSize}{parameters}",
                        HttpAccept.Rss);
                }
            }
        }

        private static string NewsnabifyTitle(string title)
        {
            return title.Replace("+", "%20");
        }
    }
}
