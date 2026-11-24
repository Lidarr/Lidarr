namespace NzbDrone.Core.Statistics
{
    public class LargestArtistStatistics
    {
        public int ArtistId { get; set; }
        public string ArtistName { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }
}
