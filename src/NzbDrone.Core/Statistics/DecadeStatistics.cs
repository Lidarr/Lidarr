namespace NzbDrone.Core.Statistics
{
    public class DecadeStatistics
    {
        public int? Decade { get; set; }
        public int AlbumCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }
}
