using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class SecondaryTypeStatisticsResource
    {
        public string SecondaryType { get; set; }
        public int AlbumCount { get; set; }
    }

    public static class SecondaryTypeStatisticsResourceMapper
    {
        public static SecondaryTypeStatisticsResource MapToResource(this SecondaryTypeStatistics model)
        {
            return new SecondaryTypeStatisticsResource
            {
                SecondaryType = model.SecondaryType,
                AlbumCount = model.AlbumCount
            };
        }
    }
}
