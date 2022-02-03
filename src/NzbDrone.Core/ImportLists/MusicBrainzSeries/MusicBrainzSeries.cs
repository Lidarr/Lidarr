using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists.MusicBrainzSeries;

public class MusicBrainzSeries : HttpImportListBase<MusicBrainzSeriesSettings>
{
    public override string Name => "MusicBrainz Series";

    public override ProviderMessage Message => new ProviderMessage("MusicBrainz Series only supports release groups within series, other types of member will not be picked up by Lidarr", ProviderMessageType.Warning);

    public override ImportListType ListType => ImportListType.Other;

    private readonly IMetadataRequestBuilder _requestBuilder;

    public MusicBrainzSeries(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, IMetadataRequestBuilder requestBuilder, Logger logger)
        : base(httpClient, importListStatusService, configService, parsingService, logger)
    {
        _requestBuilder = requestBuilder;
    }

    public override IImportListRequestGenerator GetRequestGenerator()
    {
        return new MusicBrainzSeriesRequestGenerator(_requestBuilder) { Settings = Settings };
    }

    public override IParseImportListResponse GetParser()
    {
        return new MusicBrainzSeriesParser(Settings);
    }
}
