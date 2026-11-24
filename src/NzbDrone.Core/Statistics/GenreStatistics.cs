namespace NzbDrone.Core.Statistics
{
    public class GenreStatistics
    {
        public string Genre { get; set; }
        public int AlbumCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }
}
