using Lidarr.Http.REST;

namespace Lidarr.Api.V1.Queue
{
    public class QueueStatusResource : RestResource
    {
        public int Count { get; set; }
        public int UnknownCount { get; set; }
        public bool Errors { get; set; }
        public bool Warnings { get; set; }
    }
}
