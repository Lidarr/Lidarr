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
        public RedactedSettings Settings { get; set; }

        public IHttpClient HttpClient { get; set; }
        public Logger Logger { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(null));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(string.Format("&artistname={0}&groupname={1}", searchCriteria.ArtistQuery, searchCriteria.AlbumQuery)));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(string.Format("&artistname={0}", searchCriteria.ArtistQuery)));
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
                Logger.Debug("Redacted authentication failed.");
                throw new Exception("Failed to authenticate with Redacted.");
            }

            Logger.Debug("Redacted authentication succeeded.");

            Settings.PassKey = index.Response.Passkey;
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

            var indexResponse = HttpClient.Execute(request);

            var result = Json.Deserialize<GazelleAuthResponse>(indexResponse.Content);

            return result;
        }

        private HttpRequestBuilder RequestBuilder()
        {
            return new HttpRequestBuilder($"{Settings.BaseUrl.Trim().TrimEnd('/')}")
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", Settings.ApiKey);
        }
    }
}
