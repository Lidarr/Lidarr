using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Profiles.Delay
{
    public class DelayProfileProtocolItem : IEmbeddedDocument
    {
        public string Name { get; set; }
        public string Protocol { get; set; }
        public bool Allowed { get; set; }
        public int Delay { get; set; }
    }
}
