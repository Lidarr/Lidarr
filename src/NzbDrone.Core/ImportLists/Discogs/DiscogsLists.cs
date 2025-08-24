using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.Discogs
{
    public class DiscogsLists : HttpImportListBase<DiscogsListsSettings>
    {
        public override string Name => "Discogs Lists";
        public override ImportListType ListType => ImportListType.Discogs;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(1); // 60 requests per minute limit
        public override int PageSize => 0; // Discogs doesn't support pagination for lists

        public DiscogsLists(IHttpClient httpClient,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new DiscogsListsRequestGenerator { Settings = Settings };
        }

        public override IParseImportListResponse GetParser()
        {
            var parser = new DiscogsListsParser();
            parser.SetContext(_httpClient, Settings);
            return parser;
        }
    }
}
