using System.Collections.Generic;

namespace NzbDrone.Core.Statistics
{
    public class StatisticsFilter
    {
        public List<string> RootFolderPaths { get; set; }
        public bool RootFolderPathsNot { get; set; }
        public List<int> TagIds { get; set; }
        public bool TagIdsNot { get; set; }
        public List<int> QualityProfileIds { get; set; }
        public bool QualityProfileIdsNot { get; set; }
        public List<int> MetadataProfileIds { get; set; }
        public bool MetadataProfileIdsNot { get; set; }
        public bool? Monitored { get; set; }
    }
}
