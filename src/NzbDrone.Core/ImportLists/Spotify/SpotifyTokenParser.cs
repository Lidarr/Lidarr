using NzbDrone.Core.ImportLists.Exceptions;
using System.Net;
using NzbDrone.Common.Serializer;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyTokenParser
    {

        public SpotifyToken ParseResponse(HttpResponse tokenResponse)
        {
            if (!PreProcess(tokenResponse))
            {
                return null;
            }

            return Json.Deserialize<SpotifyToken>(tokenResponse.Content);
        }

        protected virtual bool PreProcess(HttpResponse tokenResponse)
        {
            if (tokenResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListTokenException($"Error getting Spotify API token. Status code : {tokenResponse.StatusCode}");
            }

            if (tokenResponse.Headers.ContentType != null && tokenResponse.Headers.ContentType.Contains("text/json"))
            {
                throw new ImportListTokenException("Spotify responded to a token request with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }

    }
}
