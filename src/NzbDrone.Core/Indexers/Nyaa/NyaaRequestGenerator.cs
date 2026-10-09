using System;
using System.Collections.Generic;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Nyaa
{
    public class NyaaRequestGenerator : IIndexerRequestGenerator
    {
        public NyaaSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(null));

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var albumQuery = searchCriteria.CleanAlbumQuery.Replace("+", " ").Trim();

            foreach (var artistTitle in searchCriteria.CleanArtistTitles)
            {
                var artistQuery = artistTitle.Replace("+", " ").Trim();
                pageableRequests.Add(GetPagedRequests(PrepareQuery($"{artistQuery} {albumQuery}")));
            }

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            foreach (var artistTitle in searchCriteria.CleanArtistTitles)
            {
                var artistQuery = artistTitle.Replace("+", " ").Trim();
                pageableRequests.Add(GetPagedRequests(PrepareQuery(artistQuery)));
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term)
        {
            var baseUrl = $"{Settings.BaseUrl.TrimEnd('/')}/?page=rss{Settings.AdditionalParameters}";

            if (term != null)
            {
                baseUrl += "&term=" + term;
            }

            yield return new IndexerRequest(baseUrl, HttpAccept.Rss);
        }

        private string PrepareQuery(string query)
        {
            return Uri.EscapeDataString(query);
        }
    }
}
