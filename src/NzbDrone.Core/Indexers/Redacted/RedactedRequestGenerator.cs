using System.Collections.Generic;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Redacted
{
    public class RedactedRequestGenerator : IIndexerRequestGenerator
    {
        private readonly RedactedSettings _settings;

        public RedactedRequestGenerator(RedactedSettings settings)
        {
            _settings = settings;
        }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(null));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.CleanArtistQuery == "VA")
            {
                pageableRequests.Add(GetRequest($"&groupname={searchCriteria.CleanAlbumQuery}"));
            }
            else
            {
                pageableRequests.Add(GetRequest($"&artistname={searchCriteria.CleanArtistQuery}&groupname={searchCriteria.CleanAlbumQuery}"));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest($"&artistname={searchCriteria.CleanArtistQuery}"));
            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var req = RequestBuilder()
                .Resource($"ajax.php?action=browse&searchstr={searchParameters}")
                .Build();

            yield return new IndexerRequest(req);
        }

        private HttpRequestBuilder RequestBuilder()
        {
            return new HttpRequestBuilder($"{_settings.BaseUrl.Trim().TrimEnd('/')}")
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", _settings.ApiKey);
        }
    }
}
