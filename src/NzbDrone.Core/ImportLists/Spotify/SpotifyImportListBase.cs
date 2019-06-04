using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public abstract class SpotifyImportListBase<TSettings> : HttpImportListWithExpiringTokenBase<TSettings, SpotifyToken>
        where TSettings: SpotifySettingsBase<TSettings>, new()
    {
        public SpotifyImportListBase(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public abstract SpotifyRequestGeneratorBase<TSettings> GetSpotifyRequestGenerator();

        public override ImportListRequestGeneratorWithExpiringTokenBase<SpotifyToken> GetRequestGeneratorWithExpiringToken()
        {
            return GetSpotifyRequestGenerator();
        }

        public override SpotifyToken ParseResponseForToken(HttpResponse tokenResponse)
        {
            return new SpotifyTokenParser().ParseResponse(tokenResponse);
        }
    }
}
