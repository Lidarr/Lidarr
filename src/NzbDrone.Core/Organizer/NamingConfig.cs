using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameTracks = false,
            ReplaceIllegalCharacters = true,
            StandardTrackFormat = "{Artist Name} - {track:00} - {Album Title} - {Track Title}",
            ArtistFolderFormat = "{Artist Name}",
            AlbumFolderFormat = "{Album Title} ({Release Year})"
        };

        public bool RenameTracks { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public string StandardTrackFormat { get; set; }
        public string ArtistFolderFormat { get; set; }
        public string AlbumFolderFormat { get; set; }
    }
}
