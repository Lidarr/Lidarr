using System;
using Newtonsoft.Json;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyAuthorizationCodeRefreshResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        public string Scope { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsExpired { get => CreatedAt.AddSeconds(ExpiresIn) <= DateTime.UtcNow; }
    }
}
