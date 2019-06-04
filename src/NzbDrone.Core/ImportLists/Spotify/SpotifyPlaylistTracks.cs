using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyPlaylistTracks : SpotifyImportListBase<SpotifyPlaylistTracksSettings>
    {
        public override string Name => "Spotify playlist tracks";

        public override int PageSize => 1000;

        public SpotifyPlaylistTracks(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public override SpotifyRequestGeneratorBase<SpotifyPlaylistTracksSettings> GetSpotifyRequestGenerator()
        {
            return new SpotifyPlaylistTracksRequestGenerator { Settings = Settings };
        }

        public override IParseImportListResponse GetParser()
        {
            return new SpotifyPlaylistTracksParser();
        }
    }
}
