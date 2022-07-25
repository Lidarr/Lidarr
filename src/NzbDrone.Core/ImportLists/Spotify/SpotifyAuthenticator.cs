using System;
using System.Threading.Tasks;
using NzbDrone.Common.Extensions;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyAuthenticator : IAuthenticator
    {
        private readonly AuthorizationCodeTokenResponse _token;
        private readonly Func<AuthorizationCodeRefreshResponse> _refreshToken;

        public SpotifyAuthenticator(AuthorizationCodeTokenResponse token, Func<AuthorizationCodeRefreshResponse> refreshToken)
        {
            _token = token;
            _refreshToken = refreshToken;
        }

        public Task Apply(IRequest request, IAPIConnector apiConnector)
        {
            if (_token.AccessToken.IsNullOrWhiteSpace() || _token.IsExpired)
            {
                var refreshedToken = _refreshToken();

                _token.AccessToken = refreshedToken.AccessToken;
                _token.CreatedAt = refreshedToken.CreatedAt;
                _token.ExpiresIn = refreshedToken.ExpiresIn;
                _token.Scope = refreshedToken.Scope;
                _token.TokenType = refreshedToken.TokenType;
            }

            request.Headers["Authorization"] = $"{_token.TokenType} {_token.AccessToken}";

            return Task.CompletedTask;
        }
    }
}
