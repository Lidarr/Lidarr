using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileList : HttpIndexerBase<FileListSettings>
    {
        public override string Name => "FileList";
        public override string Protocol => nameof(TorrentDownloadProtocol);
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;

        private readonly IBuildSearchQuery _searchQueryBuilder;

        public FileList(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, IBuildSearchQuery searchQueryBuilder, Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
            _searchQueryBuilder = searchQueryBuilder;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new FileListRequestGenerator()
            {
                Settings = Settings,
                SearchQueryBuilder = _searchQueryBuilder
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new FileListParser(Settings);
        }
    }
}
