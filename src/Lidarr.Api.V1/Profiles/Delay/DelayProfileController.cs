using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Lidarr.Http.Validation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Delay;

namespace Lidarr.Api.V1.Profiles.Delay
{
    [V1ApiController]
    public class DelayProfileController : RestController<DelayProfileResource>
    {
        private readonly IDelayProfileService _delayProfileService;

        public DelayProfileController(IDelayProfileService delayProfileService, DelayProfileTagInUseValidator tagInUseValidator)
        {
            _delayProfileService = delayProfileService;

            SharedValidator.RuleFor(d => d.Tags).NotEmpty().When(d => d.Id != 1);
            SharedValidator.RuleFor(d => d.Tags).EmptyCollection<DelayProfileResource, int>().When(d => d.Id == 1);
            SharedValidator.RuleFor(d => d.Tags).SetValidator(tagInUseValidator);
            SharedValidator.RuleFor(d => d.Items).Must(items => items.All(x => x.Delay >= 0)).WithMessage("Protocols cannot have a negative delay");
            SharedValidator.RuleFor(d => d.Items).Must(items => items.Any(x => x.Allowed)).WithMessage("At least one protocol must be enabled");
        }

        [RestPostById]
        public ActionResult<DelayProfileResource> Create(DelayProfileResource resource)
        {
            var model = resource.ToModel();
            model = _delayProfileService.Add(model);

            return Created(model.Id);
        }

        [RestDeleteById]
        public void DeleteProfile(int id)
        {
            if (id == 1)
            {
                throw new MethodNotAllowedException("Cannot delete global delay profile");
            }

            _delayProfileService.Delete(id);
        }

        [RestPutById]
        public ActionResult<DelayProfileResource> Update(DelayProfileResource resource)
        {
            var model = resource.ToModel();
            _delayProfileService.Update(model);
            return Accepted(model.Id);
        }

        public override DelayProfileResource GetResourceById(int id)
        {
            return _delayProfileService.Get(id).ToResource();
        }

        [HttpGet]
        public List<DelayProfileResource> GetAll()
        {
            return _delayProfileService.All().ToResource();
        }

        [HttpPut("reorder/{id:int}")]
        public object Reorder(int id, [FromQuery] int? afterId = null)
        {
            ValidateId(id);

            return _delayProfileService.Reorder(id, afterId).ToResource();
        }
    }
}
