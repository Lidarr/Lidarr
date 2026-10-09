using System.Collections.Generic;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Lidarr.Http.Validation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Tagging;

namespace Lidarr.Api.V1.Profiles.Tagging
{
    [V1ApiController]
    public class TaggingProfileController : RestController<TaggingProfileResource>
    {
        private readonly ITaggingProfileService _taggingProfileService;

        public TaggingProfileController(ITaggingProfileService taggingProfileService)
        {
            _taggingProfileService = taggingProfileService;

            SharedValidator.RuleFor(d => d.Name).NotEmpty();
            SharedValidator.RuleFor(d => d.Tags).EmptyCollection<TaggingProfileResource, int>().When(d => d.Id == 1);
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
