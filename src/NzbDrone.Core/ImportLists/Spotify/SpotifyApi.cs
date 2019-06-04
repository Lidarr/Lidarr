using Newtonsoft.Json;
using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        public override string ToString()
        {
            return $"{TokenType} {AccessToken}";
        }
    }

    public class SpotifyPlaylistTracksReponse
    {
        [JsonProperty("items")]
        public IEnumerable<SpotifyTrack> Tracks { get; set; }
    }

    public class SpotifyTrack
    {
        public SpotifyTrackContent Track { get; set; }
    }

    public class SpotifyTrackContent
    {
        public SpotifyAlbum Album { get; set; }

        public IEnumerable<SpotifyArtist> Artists { get; set; }
    }

    public class SpotifyAlbum
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }

    public class SpotifyArtist
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}
