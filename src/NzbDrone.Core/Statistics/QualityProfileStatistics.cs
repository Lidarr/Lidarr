namespace NzbDrone.Core.Statistics
{
    public class QualityProfileStatistics
    {
        public int QualityProfileId { get; set; }
        public string Name { get; set; }
        public int ArtistCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }
}
