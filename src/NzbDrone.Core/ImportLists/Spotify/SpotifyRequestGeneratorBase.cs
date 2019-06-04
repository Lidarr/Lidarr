using NzbDrone.Common.Http;
using System.Net;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public abstract class SpotifyRequestGeneratorBase<TSettings> : ImportListRequestGeneratorWithExpiringTokenBase<SpotifyToken>
        where TSettings : SpotifySettingsBase<TSettings>
    {
        private const string SPOTIFY_ACCOUNTS_API_BASE_URL = "https://accounts.spotify.com/api/";

        public TSettings Settings { get; set; }

        public int MaxPages { get; set; }
        public int PageSize { get; set; }

        public SpotifyRequestGeneratorBase()
        {
            MaxPages = 1;
            PageSize = 100;
        }

        public abstract ImportListPageableRequestChain GetSpotifyListItems();

        public override ImportListPageableRequestChain GetListItemsWithExpiringToken()
        {
            return GetSpotifyListItems();
        }

        protected override ImportListRequest AddTokenToRequest(ImportListRequest importListRequest)
        {
            importListRequest.HttpRequest.Headers.Add(HttpRequestHeader.Authorization.ToString(), $"{Token.TokenType} {Token.AccessToken}");
            return importListRequest;
        }

        public override HttpRequest GetRefreshTokenRequest()
        {
            var requestBuilder = new HttpRequestBuilder(SPOTIFY_ACCOUNTS_API_BASE_URL)
                .Resource("token")
                .Post()
                .AddFormParameter("grant_type", "client_credentials");
            requestBuilder.Headers.ContentType = "application/x-www-form-urlencoded";
            var tokenRequest = requestBuilder.Build();
            tokenRequest.AddBasicAuthentication(Settings.ClientId, Settings.ClientSecret);
            return tokenRequest;
        }
    }
}
