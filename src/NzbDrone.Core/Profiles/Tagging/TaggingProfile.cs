using System.Collections.Generic;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Profiles.Tagging
{
    public class TaggingProfile : ModelBase
    {
        public string Name { get; set; }
        public HashSet<int> Tags { get; set; }
        public int Order { get; set; }
        public WriteAudioTagsType WriteAudioTags { get; set; }
        public bool ScrubAudioTags { get; set; }
        public bool EmbedCoverArt { get; set; }
        public List<int> DownloadClientIds { get; set; }
        public bool SkipHardlinkedFiles { get; set; }

        public TaggingProfile()
        {
            Tags = new HashSet<int>();
            DownloadClientIds = new List<int>();
            EmbedCoverArt = true;
        }
    }
}
