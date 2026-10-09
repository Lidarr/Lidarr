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
            MaxPages = 10; // Allow fetching up to 10 pages
            PageSize = 50; // Discogs API supports pagination with page and per_page parameters (max 100 per page)
        }

        public virtual ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();
            pageableRequests.Add(GetPagedRequests());
            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetPagedRequests()
        {
            for (var page = 1; page <= MaxPages; page++)
            {
                var request = new HttpRequestBuilder(Settings.BaseUrl.TrimEnd('/'))
                    .Resource($"/users/{Settings.Username}/wants")
                    .AddQueryParam("page", page)
                    .AddQueryParam("per_page", PageSize)
                    .SetHeader("Authorization", $"Discogs token={Settings.Token}")
                    .Build();

                yield return new ImportListRequest(request);
            }
        }
    }
}
