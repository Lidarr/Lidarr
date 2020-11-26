using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Profiles.Delay
{
    public class DelayProfileProtocolItem : IEmbeddedDocument
    {
        public string Name { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public bool Allowed { get; set; }
        public int Delay { get; set; }
    }
}
