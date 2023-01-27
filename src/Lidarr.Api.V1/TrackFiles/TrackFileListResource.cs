using System.Collections.Generic;
using NzbDrone.Core.Qualities;

namespace Lidarr.Api.V1.TrackFiles
{
    public class TrackFileListResource
    {
        public List<int> TrackFileIds { get; set; }
        public QualityModel Quality { get; set; }
        public string SceneName { get; set; }
        public string ReleaseGroup { get; set; }
    }
}
