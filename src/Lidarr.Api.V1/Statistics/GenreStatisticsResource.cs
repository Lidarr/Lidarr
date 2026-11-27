using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class GenreStatisticsResource
    {
        public string Genre { get; set; }
        public int AlbumCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }

    public static class GenreStatisticsResourceMapper
    {
        public static GenreStatisticsResource MapToResource(this GenreStatistics model)
        {
            return new GenreStatisticsResource
            {
                Genre = model.Genre,
                AlbumCount = model.AlbumCount,
                TrackFileCount = model.TrackFileCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
