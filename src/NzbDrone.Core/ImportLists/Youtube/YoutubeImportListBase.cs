using System;
using System.Collections.Generic;
using FluentValidation.Results;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Youtube
{
    public abstract class  YoutubeImportListBase<TSettings> : ImportListBase<TSettings>
        where TSettings : YoutubeSettingsBase<TSettings>, new()
    {
        private IHttpClient _httpClient;

        protected YoutubeImportListBase(IImportListStatusService importListStatusService,
                                        IConfigService configService,
                                        IParsingService parsingService,
                                        IHttpClient httpClient,
                                        Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _httpClient = httpClient;
        }

        public override ImportListType ListType => ImportListType.Youtube;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromSeconds(1);

        public override IList<ImportListItemInfo> Fetch()
        {
            IList<YoutubeImportListItemInfo> releases = new List<YoutubeImportListItemInfo>();

            using (var service = new YouTubeService(new BaseClientService.Initializer()
                   {
                       ApiKey = ""
                   }))
            {
                releases = Fetch(service);
            }

            // TODO remap

            return CleanupListItems(releases);
        }

        public IList<YoutubeImportListItemInfo> MapYoutubeReleases(IList<YoutubeImportListItemInfo> items)
        {
            return items;
        }

        public abstract IList<YoutubeImportListItemInfo> Fetch(YouTubeService service);

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
        }

        private ValidationFailure TestConnection()
        {
            return null;
        }
    }
}
