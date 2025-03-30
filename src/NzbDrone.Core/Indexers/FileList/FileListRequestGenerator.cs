using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListRequestGenerator : IIndexerRequestGenerator
    {
        public FileListSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest("latest-torrents", Settings.Categories, ""));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var artistQuery = searchCriteria.CleanArtistQuery.Replace("+", " ").Trim();
            var albumQuery = searchCriteria.CleanAlbumQuery.Replace("+", " ").Trim();

            pageableRequests.Add(GetRequest("search-torrents", Settings.Categories, string.Format("&type=name&query={0}+{1}", Uri.EscapeDataString(artistQuery), Uri.EscapeDataString(albumQuery))));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var artistQuery = searchCriteria.CleanArtistQuery.Replace("+", " ").Trim();

            pageableRequests.Add(GetRequest("search-torrents", Settings.Categories, string.Format("&type=name&query={0}", Uri.EscapeDataString(artistQuery))));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchType, IEnumerable<int> categories, string parameters)
        {
            if (categories.Empty())
            {
                yield break;
            }

            var categoriesQuery = string.Join(",", categories.Distinct());

            var baseUrl = string.Format("{0}/api.php?action={1}&category={2}{3}", Settings.BaseUrl.TrimEnd('/'), searchType, categoriesQuery, parameters);

            var request = new IndexerRequest(baseUrl, HttpAccept.Json);
            request.HttpRequest.Credentials = new BasicNetworkCredential(Settings.Username.Trim(), Settings.Passkey.Trim());

            yield return request;
        }
    }
}
