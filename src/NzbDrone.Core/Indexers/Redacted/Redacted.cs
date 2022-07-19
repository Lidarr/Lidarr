using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Redacted
{
    public class Redacted : HttpIndexerBase<RedactedSettings>
    {
        public override string Name => "Redacted";
        public override string Protocol => nameof(TorrentDownloadProtocol);
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 50;

        public Redacted(IHttpClient httpClient,
                        IIndexerStatusService indexerStatusService,
                        IConfigService configService,
                        IParsingService parsingService,
                        Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new RedactedRequestGenerator()
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new RedactedParser(Settings);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            ((RedactedRequestGenerator)GetRequestGenerator()).Authenticate();

            base.Test(failures);
        }
    }
}
