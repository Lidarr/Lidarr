using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Music
{
    public class PlaylistEntry : ModelBase
    {
        public int PlaylistId { get; set; }
        public int Order { get; set; }
        public string ForeignAlbumId { get; set; }
        public string TrackTitle { get; set; }
    }
}
