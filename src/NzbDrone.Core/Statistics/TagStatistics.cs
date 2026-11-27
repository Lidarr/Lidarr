namespace NzbDrone.Core.Statistics
{
    public class TagStatistics
    {
        public int TagId { get; set; }
        public string Label { get; set; }
        public int ArtistCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }
}
