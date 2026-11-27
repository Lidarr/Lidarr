using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class QualityProfileStatisticsResource
    {
        public int QualityProfileId { get; set; }
        public string Name { get; set; }
        public int ArtistCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }

    public static class QualityProfileStatisticsResourceMapper
    {
        public static QualityProfileStatisticsResource MapToResource(this QualityProfileStatistics model)
        {
            return new QualityProfileStatisticsResource
            {
                QualityProfileId = model.QualityProfileId,
                Name = model.Name,
                ArtistCount = model.ArtistCount,
                TrackFileCount = model.TrackFileCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
