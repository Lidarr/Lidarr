using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Redacted
{
    public class RedactedRequestGenerator : IIndexerRequestGenerator
    {
        private readonly RedactedSettings _settings;
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public RedactedRequestGenerator(RedactedSettings settings, IHttpClient httpClient, Logger logger)
        {
            _settings = settings;
            _httpClient = httpClient;
            _logger = logger;
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

        public void Authenticate()
        {
            var index = GetIndex();

            if (index == null ||
                index.Status.IsNullOrWhiteSpace() ||
                index.Status != "success" ||
                index.Response.Passkey.IsNullOrWhiteSpace())
            {
                _logger.Debug("Redacted authentication failed.");
                throw new Exception("Failed to authenticate with Redacted.");
            }

            _logger.Debug("Redacted authentication succeeded.");

            _settings.PassKey = index.Response.Passkey;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var req = RequestBuilder()
                .Resource($"ajax.php?action=browse&searchstr={searchParameters}")
                .Build();

            yield return new IndexerRequest(req);
        }

        private GazelleAuthResponse GetIndex()
        {
            var request = RequestBuilder().Resource("ajax.php?action=index").Build();

            var indexResponse = _httpClient.Execute(request);

            var result = Json.Deserialize<GazelleAuthResponse>(indexResponse.Content);

            return result;
        }

        private HttpRequestBuilder RequestBuilder()
        {
            return new HttpRequestBuilder($"{_settings.BaseUrl.Trim().TrimEnd('/')}")
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", _settings.ApiKey);
        }
    }
}
