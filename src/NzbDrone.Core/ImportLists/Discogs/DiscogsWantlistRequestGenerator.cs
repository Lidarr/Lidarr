using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Discogs
{
    public class DiscogsWantlistRequestGenerator : IImportListRequestGenerator
    {
        public DiscogsWantlistSettings Settings { get; set; }

        public int MaxPages { get; set; }
        public int PageSize { get; set; }

        public DiscogsWantlistRequestGenerator()
        {
            MaxPages = 1;
            PageSize = 0; // Discogs doesn't support pagination for wantlists currently
        }

        public virtual ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();
            pageableRequests.Add(GetPagedRequests());
            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetPagedRequests()
        {
            var request = new HttpRequestBuilder(Settings.BaseUrl.TrimEnd('/'))
                .Resource($"/users/{Settings.Username}/wants")
                .SetHeader("Authorization", $"Discogs token={Settings.Token}")
                .Build();

            yield return new ImportListRequest(request);
        }
    }
}
