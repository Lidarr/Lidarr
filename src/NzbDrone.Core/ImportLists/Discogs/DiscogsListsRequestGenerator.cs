using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Discogs
{
    public class DiscogsListsRequestGenerator : IImportListRequestGenerator
    {
        private readonly DiscogsListsSettings _settings;

        public DiscogsListsRequestGenerator(DiscogsListsSettings settings)
        {
            _settings = settings;
        }

        public virtual ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();
            pageableRequests.Add(GetPagedRequests());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetPagedRequests()
        {
            var request = new HttpRequestBuilder(_settings.BaseUrl.TrimEnd('/'))
                .Resource($"/lists/{_settings.ListId}")
                .SetHeader("Authorization", $"Discogs token={_settings.Token}")
                .Build();

            yield return new ImportListRequest(request);
        }
    }
}
