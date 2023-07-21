using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Music
{
    public class MonitoringOptions : IEmbeddedDocument
    {
        public MonitoringOptions()
        {
            Monitor = MonitorTypes.Unknown;
            AlbumsToMonitor = new List<string>();
        }

        public MonitorTypes Monitor { get; set; }
        public List<string> AlbumsToMonitor { get; set; }
        public bool Monitored { get; set; }
    }
}
