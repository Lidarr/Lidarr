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
        public bool BypassIfHighestQuality { get; set; }
        public bool BypassIfAboveCustomFormatScore { get; set; }
        public int MinimumCustomFormatScore { get; set; }
        public HashSet<int> Tags { get; set; }

        public DelayProfile()
        {
            Items = new List<DelayProfileProtocolItem>
            {
                new DelayProfileProtocolItem
                {
                    Name = "Usenet",
                    Protocol = nameof(UsenetDownloadProtocol),
                    Allowed = true
                },
                new DelayProfileProtocolItem
                {
                    Name = "Torrent",
                    Protocol = nameof(TorrentDownloadProtocol),
                    Allowed = true
                }
            };

            Tags = new HashSet<int>();
        }

        public bool IsPreferredProtocol(string protocol)
        {
            return Items.First().Protocol == protocol;
        }

        public bool IsAllowedProtocol(string protocol)
        {
            return Items.FirstOrDefault(x => x.Protocol == protocol)?.Allowed ?? false;
        }

        public int GetProtocolDelay(string protocol)
        {
            return Items.SingleOrDefault(x => x.Protocol == protocol)?.Delay ?? 0;
        }
    }
}
