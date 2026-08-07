using System;
using Lidarr.Http.REST;
using NzbDrone.Core.Configuration;

namespace Lidarr.Api.V1.Config
{
    public class MetadataProviderConfigResource : RestResource
    {
        public string MetadataSource { get; set; }

        [Obsolete("Use tagging profiles instead")]
        public WriteAudioTagsType WriteAudioTags { get; set; }

        [Obsolete("Use tagging profiles instead")]
        public bool ScrubAudioTags { get; set; }

        [Obsolete("Use tagging profiles instead")]
        public bool EmbedCoverArt { get; set; }
    }

    public static class MetadataProviderConfigResourceMapper
    {
        public static MetadataProviderConfigResource ToResource(IConfigService model)
        {
            return new MetadataProviderConfigResource
            {
                MetadataSource = model.MetadataSource,
            };
        }
    }
}
