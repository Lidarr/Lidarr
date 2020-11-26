using System.Collections.Generic;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Deemix
{
    public class DeemixRequestGenerator : IIndexerRequestGenerator
    {
        private const int PageSize = 100;
        private const int MaxPages = 30;
        public DeemixIndexerSettings Settings { get; set; }
        public Logger Logger { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(new[]
            {
                new DeemixRequest(
                    Settings.BaseUrl,
                    x => x.RecentReleases())
            });

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();

            chain.AddTier(GetRequests(string.Format("artist:\"{0}\" album:\"{1}\"", searchCriteria.ArtistQuery, searchCriteria.AlbumQuery)));
            chain.AddTier(GetRequests(string.Format("{0} {1}", searchCriteria.ArtistQuery, searchCriteria.AlbumQuery)));

            return chain;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();

            chain.AddTier(GetRequests(string.Format("artist:\"{0}\"", searchCriteria.ArtistQuery)));
            chain.AddTier(GetRequests(searchCriteria.ArtistQuery));

            return chain;
        }

        private IEnumerable<IndexerRequest> GetRequests(string searchParameters)
        {
            for (var page = 0; page < MaxPages; page++)
            {
                var request =
                    new DeemixRequest(
                        Settings.BaseUrl,
                        x => x.Search(searchParameters, PageSize, page * PageSize));

                yield return request;
            }
        }
    }
}
