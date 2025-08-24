using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.Discogs
{
    public class DiscogsWantlist : HttpImportListBase<DiscogsWantlistSettings>
    {
        public override string Name => "Discogs Wantlist";
        public override ImportListType ListType => ImportListType.Discogs;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(3); // Conservative rate limiting to avoid 429 errors when fetching many releases
        public override int PageSize => 0; // Discogs doesn't support pagination for wantlists currently

        public DiscogsWantlist(IHttpClient httpClient,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new DiscogsWantlistRequestGenerator { Settings = Settings };
        }

        public override IParseImportListResponse GetParser()
        {
            var parser = new DiscogsWantlistParser();
            parser.SetContext(_httpClient, Settings);
            return parser;
        }
    }
}
