using System.Collections.Generic;
using System.Linq;
using Lidarr.Http.REST;
using NzbDrone.Core.Profiles.Delay;

namespace Lidarr.Api.V1.Profiles.Delay
{
    public class DelayProfileResource : RestResource
    {
        public string Name { get; set; }
        public List<DelayProfileProtocolItemResource> Items { get; set; }
        public bool BypassIfHighestQuality { get; set; }
        public bool BypassIfAboveCustomFormatScore { get; set; }
        public int MinimumCustomFormatScore { get; set; }
        public int Order { get; set; }
        public HashSet<int> Tags { get; set; }
    }

    public static class DelayProfileResourceMapper
    {
        public static DelayProfileResource ToResource(this DelayProfile model)
        {
            if (model == null)
            {
                return null;
            }

            return new DelayProfileResource
            {
                Id = model.Id,
                Name = model.Name,
                Items = model.Items.Select(x => x.ToResource()).ToList(),
                BypassIfHighestQuality = model.BypassIfHighestQuality,
                BypassIfAboveCustomFormatScore = model.BypassIfAboveCustomFormatScore,
                MinimumCustomFormatScore = model.MinimumCustomFormatScore,
                Order = model.Order,
                Tags = new HashSet<int>(model.Tags)
            };
        }

        public static DelayProfile ToModel(this DelayProfileResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new DelayProfile
            {
                Id = resource.Id,
                Name = resource.Name,
                Items = resource.Items.Select(x => x.ToModel()).ToList(),
                BypassIfHighestQuality = resource.BypassIfHighestQuality,
                BypassIfAboveCustomFormatScore = resource.BypassIfAboveCustomFormatScore,
                MinimumCustomFormatScore = resource.MinimumCustomFormatScore,
                Order = resource.Order,
                Tags = new HashSet<int>(resource.Tags)
            };
        }

        public static List<DelayProfileResource> ToResource(this IEnumerable<DelayProfile> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
