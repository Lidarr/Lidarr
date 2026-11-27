using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class TagStatisticsResource
    {
        public int TagId { get; set; }
        public string Label { get; set; }
        public int ArtistCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
    }

    public static class TagStatisticsResourceMapper
    {
        public static TagStatisticsResource MapToResource(this TagStatistics model)
        {
            return new TagStatisticsResource
            {
                TagId = model.TagId,
                Label = model.Label,
                ArtistCount = model.ArtistCount,
                TrackFileCount = model.TrackFileCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
