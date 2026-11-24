using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class AlbumTypeStatisticsResource
    {
        public string AlbumType { get; set; }
        public int AlbumCount { get; set; }
    }

    public static class AlbumTypeStatisticsResourceMapper
    {
        public static AlbumTypeStatisticsResource MapToResource(this AlbumTypeStatistics model)
        {
            return new AlbumTypeStatisticsResource
            {
                AlbumType = model.AlbumType,
                AlbumCount = model.AlbumCount
            };
        }
    }
}
