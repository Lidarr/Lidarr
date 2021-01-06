using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameTracks = false,
            ReplaceIllegalCharacters = true,
            StandardTrackFormat = "{Album Title} ({Release Year})/{Artist Name} - {Album Title} - {track:00} - {Track Title}",
            MultiDiscTrackFormat = "{Album Title} ({Release Year})/{Medium Format} {medium:00}/{Artist Name} - {Album Title} - {track:00} - {Track Title}",
            ArtistFolderFormat = "{Artist Name}",
        };

        public bool RenameTracks { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public string StandardTrackFormat { get; set; }
        public string MultiDiscTrackFormat { get; set; }
        public string ArtistFolderFormat { get; set; }
    }
}
