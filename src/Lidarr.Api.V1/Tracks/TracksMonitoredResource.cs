using System.Collections.Generic;

namespace Lidarr.Api.V1.Tracks
{
    public class TracksMonitoredResource
    {
        public List<int> TrackIds { get; set; }
        public bool Monitored { get; set; }
    }
}
