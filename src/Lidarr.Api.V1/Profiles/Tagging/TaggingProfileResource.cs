using System.Collections.Generic;
using System.Linq;
using Lidarr.Http.REST;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Profiles.Tagging;

namespace Lidarr.Api.V1.Profiles.Tagging
{
    public class TaggingProfileResource : RestResource
    {
        public string Name { get; set; }
        public WriteAudioTagsType WriteAudioTags { get; set; }
        public bool ScrubAudioTags { get; set; }
        public bool EmbedCoverArt { get; set; }
        public int Order { get; set; }
        public HashSet<int> Tags { get; set; }
        public bool SkipHardlinkedFiles { get; set; } = true;
    }

    public static class TaggingProfileResourceMapper
    {
        public static TaggingProfileResource ToResource(this TaggingProfile model)
        {
            if (model == null)
            {
                return null;
            }

            return new TaggingProfileResource
            {
                Id = model.Id,
                Name = model.Name,
                WriteAudioTags = model.WriteAudioTags,
                ScrubAudioTags = model.ScrubAudioTags,
                EmbedCoverArt = model.EmbedCoverArt,
                Order = model.Order,
                Tags = new HashSet<int>(model.Tags),
                SkipHardlinkedFiles = model.SkipHardlinkedFiles
            };
        }

        public static TaggingProfile ToModel(this TaggingProfileResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new TaggingProfile
            {
                Id = resource.Id,
                Name = resource.Name,
                WriteAudioTags = resource.WriteAudioTags,
                ScrubAudioTags = resource.ScrubAudioTags,
                EmbedCoverArt = resource.EmbedCoverArt,
                Order = resource.Order,
                Tags = new HashSet<int>(resource.Tags),
                SkipHardlinkedFiles = resource.SkipHardlinkedFiles
            };
        }

        public static List<TaggingProfileResource> ToResource(this IEnumerable<TaggingProfile> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
