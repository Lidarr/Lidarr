using System.Collections.Generic;
using System.Linq;
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

            foreach (var artistTitle in searchCriteria.CleanArtistTitles)
            {
                if (artistTitle == "VA")
                {
                    pageableRequests.Add(GetRequest($"groupname={searchCriteria.CleanAlbumQuery}"));
                }
                else
                {
                    pageableRequests.Add(GetRequest($"artistname={artistTitle}&groupname={searchCriteria.CleanAlbumQuery}"));
                }
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            foreach (var artistTitle in searchCriteria.CleanArtistTitles)
            {
                pageableRequests.Add(GetRequest($"artistname={artistTitle}"));
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var requestBuilder = RequestBuilder()
                .Resource($"ajax.php?{searchParameters}")
                .AddQueryParam("action", "browse")
                .AddQueryParam("order_by", "time")
                .AddQueryParam("order_way", "desc");

            var categories = _settings.Categories.ToList();

            if (categories.Any())
            {
                categories.ForEach(cat => requestBuilder.AddQueryParam($"filter_cat[{cat}]", "1"));
            }

            yield return new IndexerRequest(requestBuilder.Build());
        }

        private HttpRequestBuilder RequestBuilder()
        {
            return new HttpRequestBuilder($"{_settings.BaseUrl.Trim().TrimEnd('/')}")
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", _settings.ApiKey);
        }
    }
}
