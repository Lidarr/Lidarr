using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Music
{
    public class Playlist : ModelBase
    {
        public string ForeignPlaylistId { get; set; }
        public string Title { get; set; }
        public string OutputFolder { get; set; }
        public List<PlaylistEntry> Items { get; set; }
    }
}
