using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class LargestArtistStatisticsResource
    {
        public int ArtistId { get; set; }
        public string ArtistName { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }

    public static class LargestArtistStatisticsResourceMapper
    {
        public static LargestArtistStatisticsResource MapToResource(this LargestArtistStatistics model)
        {
            return new LargestArtistStatisticsResource
            {
                ArtistId = model.ArtistId,
                ArtistName = model.ArtistName,
                TrackFileCount = model.TrackFileCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
