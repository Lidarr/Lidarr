using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.MediaFiles
{
    public class CueSheet : ModelBase
    {
        public class IndexEntry
        {
            public int Key { get; set; }
            public string Time { get; set; }
        }

        public class TrackEntry
        {
            public int Number { get; set; }
            public string Title { get; set; }
            public string Performer { get; set; }
            public List<IndexEntry> Indices { get; set; } = new List<IndexEntry>();
        }

        public class FileEntry
        {
            public string Name { get; set; }
            public IndexEntry Index { get; set; }
            public List<TrackEntry> Tracks { get; set; } = new List<TrackEntry>();
        }

        public string Path { get; set; }
        public bool IsSingleFileRelease { get; set; }
        public List<FileEntry> Files { get; set; } = new List<FileEntry>();
        public string Genre { get; set; }
        public string Date { get; set; }
        public string DiscID { get; set; }
        public string Title { get; set; }
        public string Performer { get; set; }
    }
}
