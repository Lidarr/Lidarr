using System.Collections.Generic;
using NzbDrone.Core.MetadataSource;

namespace NzbDrone.Core.ImportLists.MusicBrainzSeries;

public class MusicBrainzSeriesRequestGenerator : IImportListRequestGenerator
{
    public MusicBrainzSeriesSettings Settings { get; set; }

    private readonly IMetadataRequestBuilder _requestBulder;

    public MusicBrainzSeriesRequestGenerator(IMetadataRequestBuilder requestBuilder)
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
                                    .SetSegment("route", "series/" + Settings.SeriesId)
                                    .Build();

        yield return new ImportListRequest(request);
    }
}
