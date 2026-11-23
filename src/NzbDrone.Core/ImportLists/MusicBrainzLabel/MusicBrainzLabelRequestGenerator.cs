using System.Collections.Generic;
using NzbDrone.Core.MetadataSource;

namespace NzbDrone.Core.ImportLists.MusicBrainzLabel
{
    public class MusicBrainzLabelRequestGenerator : IImportListRequestGenerator
    {
        public MusicBrainzLabelSettings Settings { get; set; }

        private readonly IMetadataRequestBuilder _requestBulder;

        public MusicBrainzLabelRequestGenerator(IMetadataRequestBuilder requestBuilder)
        {
            _requestBulder = requestBuilder;
        }

        public virtual ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetPagedRequests());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetPagedRequests()
        {
            var request = _requestBulder.GetRequestBuilder()
                                        .Create()
                                        .SetSegment("route", "Label/" + Settings.LabelId)
                                        .Build();

            yield return new ImportListRequest(request);
        }
    }
}
