using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using System.Text;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Waffles
{
    public class WafflesRequestGenerator : IIndexerRequestGenerator
    {
        public WafflesSettings Settings { get; set; }
        
        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(null));

            return pageableRequests;
        }

        [System.Obsolete("Sonarr TV Stuff -- Shouldn't be needed for Lidarr")]
        public virtual IndexerPageableRequestChain GetSearchRequests(SingleEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        [System.Obsolete("Sonarr TV Stuff -- Shouldn't be needed for Lidarr")]
        public virtual IndexerPageableRequestChain GetSearchRequests(SeasonSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        [System.Obsolete("Sonarr TV Stuff -- Shouldn't be needed for Lidarr")]
        public virtual IndexerPageableRequestChain GetSearchRequests(DailyEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        [System.Obsolete("Sonarr TV Stuff -- Shouldn't be needed for Lidarr")]
        public virtual IndexerPageableRequestChain GetSearchRequests(AnimeEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        [System.Obsolete("Sonarr TV Stuff -- Shouldn't be needed for Lidarr")]
        public virtual IndexerPageableRequestChain GetSearchRequests(SpecialEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("&q=artist:{0} album:{1}",searchCriteria.Artist.Name,searchCriteria.Album.Title)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("&q=artist:{0}", searchCriteria.Artist.Name)));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string query)
        { 

            var url = new StringBuilder();
        
            url.AppendFormat("{0}/browse.php?rss=1&c0=1&uid={1}&passkey={2}", Settings.BaseUrl.Trim().TrimEnd('/'), Settings.UserId, Settings.RssPasskey);

            if (query.IsNotNullOrWhiteSpace())
            {
                url.AppendFormat(query);
            }

            yield return new IndexerRequest(url.ToString(), HttpAccept.Rss);
        }
    }
}
