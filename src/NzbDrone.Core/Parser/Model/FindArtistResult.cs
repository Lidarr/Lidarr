using NzbDrone.Core.Music;

namespace NzbDrone.Core.Parser.Model
{
    public class FindArtistResult
    {
        public Artist Artist { get; set; }
        public ArtistMatchType MatchType { get; set; }

        public FindArtistResult(Artist artist, ArtistMatchType matchType)
        {
            Artist = artist;
            MatchType = matchType;
        }
    }

    public enum ArtistMatchType
    {
        Unknown = 0,
        Title = 1,
        Alias = 2,
        Id = 3
    }
}
