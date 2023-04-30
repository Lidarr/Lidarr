using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Redacted
{
    public class Redacted : HttpIndexerBase<RedactedSettings>
    {
        public override string Name => "Redacted";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
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
            return new RedactedRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new RedactedParser(Settings);
        }

        public override HttpRequest GetDownloadRequest(string link)
        {
            var request = new HttpRequest(link);
            request.Headers.Set("Authorization", Settings.ApiKey);

            return request;
        }
    }
}
