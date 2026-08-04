using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists.MusicBrainzLabel
{
    public class MusicBrainzLabel : HttpImportListBase<MusicBrainzLabelSettings>
    {
        public override string Name => "MusicBrainz Label";

        public override ProviderMessage Message => new ProviderMessage(
            "Labels can be very large. Start with the default type filters and widen them once you see what the label brings in.",
            ProviderMessageType.Info);

        public override ImportListType ListType => ImportListType.Other;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        // MusicBrainz asks for no more than one request per second per client.
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(1);

        public override int PageSize => MusicBrainzLabelRequestGenerator.PageSize;

        public MusicBrainzLabel(IHttpClient httpClient,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new MusicBrainzLabelRequestGenerator(_httpClient, _logger) { Settings = Settings };
        }

        public override IParseImportListResponse GetParser()
        {
            return new MusicBrainzLabelParser(Settings, _logger);
        }

        // The base class stops paging as soon as a page yields fewer items than
        // PageSize, which assumes one item per API entity. Here the parser filters
        // by release type and folds pressings together, so a full page of 100
        // releases routinely produces a handful of albums — that heuristic would
        // silently truncate the list after page one. The request generator instead
        // asks MusicBrainz for the label's release count and emits exactly the
        // pages needed, so every page it hands us is one we want.
        protected override bool IsFullPage(IList<ImportListItemInfo> page)
        {
            return true;
        }
    }
}
