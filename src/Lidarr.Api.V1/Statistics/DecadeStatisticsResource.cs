using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class DecadeStatisticsResource
    {
        public int? Decade { get; set; }
        public int AlbumCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }

    public static class DecadeStatisticsResourceMapper
    {
        public static DecadeStatisticsResource MapToResource(this DecadeStatistics model)
        {
            return new DecadeStatisticsResource
            {
                Decade = model.Decade,
                AlbumCount = model.AlbumCount,
                TrackFileCount = model.TrackFileCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
