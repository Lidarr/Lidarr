using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Profiles.Delay
{
    public class DelayProfile : ModelBase
    {
        public string Name { get; set; }
        public List<DelayProfileProtocolItem> Items { get; set; }
        public int Order { get; set; }
        public HashSet<int> Tags { get; set; }

        public DelayProfile()
        {
            Items = new List<DelayProfileProtocolItem>
            {
                new DelayProfileProtocolItem
                {
                    Name = "Usenet",
                    Protocol = DownloadProtocol.Usenet,
                    Allowed = true
                },
                new DelayProfileProtocolItem
                {
                    Name = "Torrent",
                    Protocol = DownloadProtocol.Torrent,
                    Allowed = true
                },
                new DelayProfileProtocolItem
                {
                    Name = "Deemix",
                    Protocol = DownloadProtocol.Deemix,
                    Allowed = true
                }
            };

            Tags = new HashSet<int>();
        }

        public bool IsPreferredProtocol(DownloadProtocol protocol)
        {
            return Items.First().Protocol == protocol;
        }

        public bool IsAllowedProtocol(DownloadProtocol protocol)
        {
            return Items.First(x => x.Protocol == protocol).Allowed;
        }

        public int GetProtocolDelay(DownloadProtocol protocol)
        {
            return Items.SingleOrDefault(x => x.Protocol == protocol)?.Delay ?? 0;
        }
    }
}
