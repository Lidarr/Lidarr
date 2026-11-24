using NzbDrone.Core.Qualities;
using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class QualityStatisticsResource
    {
        public Quality Quality { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }

    public static class QualityStatisticsResourceMapper
    {
        public static QualityStatisticsResource MapToResource(this QualityStatistics model)
        {
            return new QualityStatisticsResource
            {
                Quality = model.Quality,
                TrackFileCount = model.TrackFileCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
