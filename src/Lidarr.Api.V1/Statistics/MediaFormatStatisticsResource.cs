using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class MediaFormatStatisticsResource
    {
        public string MediaFormat { get; set; }
        public int AlbumCount { get; set; }
    }

    public static class MediaFormatStatisticsResourceMapper
    {
        public static MediaFormatStatisticsResource MapToResource(this MediaFormatStatistics model)
        {
            return new MediaFormatStatisticsResource
            {
                MediaFormat = model.MediaFormat,
                AlbumCount = model.AlbumCount
            };
        }
    }
}
