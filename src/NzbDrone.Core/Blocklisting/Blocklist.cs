using System;
using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Blocklisting
{
    public class Blocklist : ModelBase
    {
        public int ArtistId { get; set; }
        public Artist Artist { get; set; }
        public List<int> AlbumIds { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public DateTime Date { get; set; }
        public DateTime? PublishedDate { get; set; }
        public long? Size { get; set; }
        public string Protocol { get; set; }
        public string Indexer { get; set; }
        public string Message { get; set; }
        public string TorrentInfoHash { get; set; }
    }
}
