using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.ImportLists.MusicBrainzLabel
{
    // Shape of a MusicBrainz browse request:
    //   /ws/2/release?label={mbid}&inc=release-groups+artist-credits
    // Labels attach to releases rather than release groups, so we browse releases
    // and fold them back up into release groups, which are what Lidarr calls albums.
    public class MusicBrainzLabelBrowseResponse
    {
        [JsonProperty("release-count")]
        public int ReleaseCount { get; set; }

        [JsonProperty("release-offset")]
        public int ReleaseOffset { get; set; }

        [JsonProperty("releases")]
        public List<MusicBrainzLabelRelease> Releases { get; set; }
    }

    public class MusicBrainzLabelRelease
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        // Official, Promotion, Bootleg or Pseudo-Release.
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("release-group")]
        public MusicBrainzLabelReleaseGroup ReleaseGroup { get; set; }

        [JsonProperty("artist-credit")]
        public List<MusicBrainzLabelArtistCredit> ArtistCredit { get; set; }
    }

    public class MusicBrainzLabelReleaseGroup
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        // Album, EP, Single, Broadcast or Other.
        [JsonProperty("primary-type")]
        public string PrimaryType { get; set; }

        // Compilation, Live, Remix, Soundtrack, DJ-mix and friends. A release
        // group can carry several at once.
        [JsonProperty("secondary-types")]
        public List<string> SecondaryTypes { get; set; }

        // May be a bare year, a year-month, or a full date.
        [JsonProperty("first-release-date")]
        public string FirstReleaseDate { get; set; }

        [JsonProperty("artist-credit")]
        public List<MusicBrainzLabelArtistCredit> ArtistCredit { get; set; }
    }

    public class MusicBrainzLabelArtistCredit
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("artist")]
        public MusicBrainzLabelArtist Artist { get; set; }
    }

    public class MusicBrainzLabelArtist
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
