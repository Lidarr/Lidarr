using System.Collections.Generic;
using System.Text.Json.Serialization;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedAlbumInfo
    {
        public string ReleaseTitle { get; set; }
        public string AlbumTitle { get; set; }
        public string ArtistName { get; set; }
        public string AlbumType { get; set; }
        public ArtistTitleInfo ArtistTitleInfo { get; set; }
        public QualityModel Quality { get; set; }
        public string ReleaseDate { get; set; }
        public int? ReleaseYear { get; set; }
        public YearMatchConfidence YearConfidence { get; set; }
        public bool Discography { get; set; }
        public int DiscographyStart { get; set; }
        public int DiscographyEnd { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }
        public string ReleaseVersion { get; set; }

        [JsonIgnore]
        public Dictionary<string, object> ExtraInfo { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            var albumString = "[Unknown Album]";

            if (AlbumTitle != null)
            {
                albumString = ReleaseYear.HasValue ? $"{AlbumTitle} ({ReleaseYear.Value})" : AlbumTitle;
            }

            return $"{ArtistName} - {albumString} {Quality}";
        }
    }
}
