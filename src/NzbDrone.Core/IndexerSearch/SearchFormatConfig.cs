using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.IndexerSearch
{
    public class SearchFormatConfig : ModelBase
    {
        public static SearchFormatConfig Default => new SearchFormatConfig
        {
            UseCustomSearchFormat = false,
            AlbumSearchFormat = "{Artist Name} {Album Title}",
            ArtistSearchFormat = "{Artist Name}",
        };

        public bool UseCustomSearchFormat { get; set; }
        public string AlbumSearchFormat { get; set; }
        public string ArtistSearchFormat { get; set; }
    }
}
