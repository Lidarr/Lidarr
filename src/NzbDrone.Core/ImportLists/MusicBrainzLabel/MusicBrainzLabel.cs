using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists.MusicBrainzLabel
{
    public class MusicBrainzLabel : HttpImportListBase<MusicBrainzLabelSettings>
    {
        public override string Name => "MusicBrainz Label";

        public override ProviderMessage Message => new ProviderMessage("MusicBrainz Label only supports release groups within Label, other types of member will not be picked up by Lidarr", ProviderMessageType.Warning);

        public override ImportListType ListType => ImportListType.Other;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        private readonly IMetadataRequestBuilder _requestBuilder;

        public MusicBrainzLabel(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, IMetadataRequestBuilder requestBuilder, Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
            _requestBuilder = requestBuilder;
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new MusicBrainzLabelRequestGenerator(_requestBuilder) { Settings = Settings };
        }

        public override IParseImportListResponse GetParser()
        {
            return new MusicBrainzLabelParser(Settings);
        }
    }
}
