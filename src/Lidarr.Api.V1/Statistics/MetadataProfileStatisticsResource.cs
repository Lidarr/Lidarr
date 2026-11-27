using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class MetadataProfileStatisticsResource
    {
        public int MetadataProfileId { get; set; }
        public string Name { get; set; }
        public int ArtistCount { get; set; }
    }

    public static class MetadataProfileStatisticsResourceMapper
    {
        public static MetadataProfileStatisticsResource MapToResource(this MetadataProfileStatistics model)
        {
            return new MetadataProfileStatisticsResource
            {
                MetadataProfileId = model.MetadataProfileId,
                Name = model.Name,
                ArtistCount = model.ArtistCount
            };
        }
    }
}
