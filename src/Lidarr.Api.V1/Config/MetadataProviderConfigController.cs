using System.Linq;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Profiles.Tagging;
using NzbDrone.Core.Validation;

namespace Lidarr.Api.V1.Config
{
    [V1ApiController("config/metadataprovider")]
    public class MetadataProviderConfigController : ConfigController<MetadataProviderConfigResource>
    {
        private readonly ITaggingProfileService _taggingProfileService;

        public MetadataProviderConfigController(IConfigService configService,
                                                ITaggingProfileService taggingProfileService)
            : base(configService)
        {
            _taggingProfileService = taggingProfileService;

            SharedValidator.RuleFor(c => c.MetadataSource).IsValidUrl().When(c => !c.MetadataSource.IsNullOrWhiteSpace());
        }

        [RestPutById]
        public override ActionResult<MetadataProviderConfigResource> SaveConfig([FromBody] MetadataProviderConfigResource resource)
        {
            var defaultProfile = GetSeededDefaultProfile();

#pragma warning disable CS0618 // Type or member is obsolete
            if (defaultProfile != null &&
                (defaultProfile.WriteAudioTags != resource.WriteAudioTags ||
                 defaultProfile.ScrubAudioTags != resource.ScrubAudioTags ||
                 defaultProfile.EmbedCoverArt != resource.EmbedCoverArt))
            {
                defaultProfile.WriteAudioTags = resource.WriteAudioTags;
                defaultProfile.ScrubAudioTags = resource.ScrubAudioTags;
                defaultProfile.EmbedCoverArt = resource.EmbedCoverArt;

                _taggingProfileService.Update(defaultProfile);
            }
#pragma warning restore CS0618 // Type or member is obsolete

            return base.SaveConfig(resource);
        }

        protected override MetadataProviderConfigResource ToResource(IConfigService model)
        {
            var resource = MetadataProviderConfigResourceMapper.ToResource(model);
            var defaultProfile = GetSeededDefaultProfile();

            if (defaultProfile != null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                resource.WriteAudioTags = defaultProfile.WriteAudioTags;
                resource.ScrubAudioTags = defaultProfile.ScrubAudioTags;
                resource.EmbedCoverArt = defaultProfile.EmbedCoverArt;
#pragma warning restore CS0618 // Type or member is obsolete
            }

            return resource;
        }

        // seeded by migration 082 and undeletable (TaggingProfileService.Delete skips Id 1)
        private TaggingProfile GetSeededDefaultProfile()
        {
            return _taggingProfileService.All().FirstOrDefault(p => p.Id == 1);
        }
    }
}
