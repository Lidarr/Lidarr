using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.Deemix;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Deemix
{
    public class Deemix : HttpIndexerBase<DeemixIndexerSettings>
    {
        private readonly IDeemixProxyManager _proxyManager;

        public override string Name => "Deemix";
        public override DownloadProtocol Protocol => DownloadProtocol.Deemix;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 100;
        public override TimeSpan RateLimit => new TimeSpan(0);

        public Deemix(IDeemixProxyManager proxyManager,
                      IHttpClient httpClient,
                      IIndexerStatusService indexerStatusService,
                      IConfigService configService,
                      IParsingService parsingService,
                      Logger logger)
        : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
            _proxyManager = proxyManager;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new DeemixRequestGenerator()
            {
                Settings = Settings,
                Logger = _logger
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return null;
        }

        protected override IList<ReleaseInfo> FetchPage(IndexerRequest request, IParseIndexerResponse parser)
        {
            var proxy = _proxyManager.GetProxy(Settings.BaseUrl);
            var deemixRequest = (DeemixRequest)request;

            var response = deemixRequest.Request(proxy);

            return DeemixParser.ParseResponse(response);
        }

        protected override IndexerResponse FetchIndexerResponse(IndexerRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
