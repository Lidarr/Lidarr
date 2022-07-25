using System.Net;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyRetryHandler : IRetryHandler
    {
        private readonly SimpleRetryHandler _simpleHandler;
        private readonly AuthorizationCodeTokenResponse _token;

        public SpotifyRetryHandler(AuthorizationCodeTokenResponse token)
        {
            _token = token;

            _simpleHandler = new SimpleRetryHandler
            {
                RetryTimes = 3,
                RetryErrorCodes = new[]
                {
                    HttpStatusCode.InternalServerError,
                    HttpStatusCode.BadGateway,
                    HttpStatusCode.ServiceUnavailable,
                    HttpStatusCode.Unauthorized
                }
            };
        }

        public Task<IResponse> HandleRetry(IRequest request, IResponse response, IRetryHandler.RetryFunc retry)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _token.ExpiresIn = -1;
            }

            return _simpleHandler.HandleRetry(request, response, retry);
        }
    }
}
