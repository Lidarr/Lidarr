using System;
using System.Collections.Generic;

namespace NzbDrone.Core.Music.Popularity
{
    public class SongPopularityResult
    {
        public string Source { get; set; } = "Deezer";
        public string Status { get; set; } = "noMatch";
        public DateTime FetchedAt { get; set; }
        public List<SongPopularity> Tracks { get; set; } = new List<SongPopularity>();
    }

    public class SongPopularity
    {
        public string ForeignRecordingId { get; set; }
        public int Position { get; set; }
        public long ProviderScore { get; set; }
        public string SourceUrl { get; set; }
    }
}
