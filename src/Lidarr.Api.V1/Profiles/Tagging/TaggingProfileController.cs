using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Lidarr.Http.Validation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Download;
using NzbDrone.Core.Profiles.Tagging;

namespace Lidarr.Api.V1.Profiles.Tagging
{
    [V1ApiController]
    public class TaggingProfileController : RestController<TaggingProfileResource>
    {
        private readonly ITaggingProfileService _taggingProfileService;
        private readonly IDownloadClientFactory _downloadClientFactory;

        public TaggingProfileController(ITaggingProfileService taggingProfileService, IDownloadClientFactory downloadClientFactory)
        {
            _taggingProfileService = taggingProfileService;
            _downloadClientFactory = downloadClientFactory;

            SharedValidator.RuleFor(d => d.Name).NotEmpty();
            SharedValidator.RuleFor(d => d.Tags).EmptyCollection<TaggingProfileResource, int>().When(d => d.Id == 1);
            SharedValidator.RuleFor(d => d).Custom((profile, context) =>
            {
                if (profile.DownloadClientIds.Any())
                {
                    foreach (var downloadClientId in profile.DownloadClientIds.Where(downloadClientId => !_downloadClientFactory.Exists(downloadClientId)))
                    {
                        context.AddFailure(nameof(TaggingProfileResource.DownloadClientIds), $"Download client does not exist: {downloadClientId}");
                    }
                }
            });
        }

        [RestPostById]
        public ActionResult<TaggingProfileResource> Create([FromBody] TaggingProfileResource resource)
        {
            var model = resource.ToModel();
            model = _taggingProfileService.Add(model);

            return Created(model.Id);
        }

        [RestDeleteById]
        public void DeleteProfile(int id)
        {
            if (id == 1)
            {
                throw new MethodNotAllowedException("Cannot delete default tagging profile");
            }

            _taggingProfileService.Delete(id);
        }

        [RestPutById]
        public ActionResult<TaggingProfileResource> Update([FromBody] TaggingProfileResource resource)
        {
            var model = resource.ToModel();
            _taggingProfileService.Update(model);
            return Accepted(model.Id);
        }

        public override TaggingProfileResource GetResourceById(int id)
        {
            return _taggingProfileService.Get(id).ToResource();
        }

        [HttpGet]
        public List<TaggingProfileResource> GetAll()
        {
            return _taggingProfileService.All().ToResource();
        }

        [HttpPut("reorder/{id:int}")]
        public object Reorder(int id, [FromQuery] int? afterId = null)
        {
            ValidateId(id);

            return _taggingProfileService.Reorder(id, afterId).ToResource();
        }
    }
}
