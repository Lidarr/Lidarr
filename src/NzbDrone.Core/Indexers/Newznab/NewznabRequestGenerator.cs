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

        private string TextSearchEngine
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.TextSearchEngine;
            }
        }

        private string AudioTextSearchEngine
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.AudioTextSearchEngine;
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
                var artistQuery = AudioTextSearchEngine == "raw" ? searchCriteria.ArtistQuery : searchCriteria.CleanArtistQuery;
                var albumQuery = AudioTextSearchEngine == "raw" ? searchCriteria.AlbumQuery : searchCriteria.CleanAlbumQuery;

                AddAudioPageableRequests(pageableRequests,
                    searchCriteria,
                    $"&artist={NewsnabifyTitle(artistQuery)}&album={NewsnabifyTitle(albumQuery)}");
            }

            if (SupportsSearch)
            {
                pageableRequests.AddTier();

                var artistQuery = TextSearchEngine == "raw" ? searchCriteria.ArtistQuery : searchCriteria.CleanArtistQuery;
                var albumQuery = TextSearchEngine == "raw" ? searchCriteria.AlbumQuery : searchCriteria.CleanAlbumQuery;

                pageableRequests.Add(GetPagedRequests(MaxPages,
                    Settings.Categories,
                    "search",
                    $"&q={NewsnabifyTitle($"{artistQuery}+{albumQuery}")}"));
            }

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (SupportsAudioSearch)
            {
                var queryTitle = AudioTextSearchEngine == "raw" ? searchCriteria.ArtistQuery : searchCriteria.CleanArtistQuery;

                AddAudioPageableRequests(pageableRequests,
                    searchCriteria,
                    $"&artist={NewsnabifyTitle(queryTitle)}");
            }

            if (SupportsSearch)
            {
                pageableRequests.AddTier();
                var queryTitle = TextSearchEngine == "raw" ? searchCriteria.ArtistQuery : searchCriteria.CleanArtistQuery;

                pageableRequests.Add(GetPagedRequests(MaxPages,
                        Settings.Categories,
                        "search",
                        $"&q={NewsnabifyTitle(queryTitle)}"));
            }

            return pageableRequests;
        }

        private void AddAudioPageableRequests(IndexerPageableRequestChain chain, SearchCriteriaBase searchCriteria, string parameters)
        {
            chain.AddTier();

            chain.Add(GetPagedRequests(MaxPages, Settings.Categories, "music", $"&q={parameters}"));
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
            title = title.Replace("+", " ");
            return Uri.EscapeDataString(title);
        }
    }
}
