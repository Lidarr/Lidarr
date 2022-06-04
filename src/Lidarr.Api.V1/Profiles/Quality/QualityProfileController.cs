using System.Collections.Generic;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Qualities;

namespace Lidarr.Api.V1.Profiles.Quality
{
    [V1ApiController]
    public class QualityProfileController : RestController<QualityProfileResource>
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileController(IQualityProfileService qualityProfileService)
        {
            _qualityProfileService = qualityProfileService;
            SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Cutoff).ValidCutoff();
            SharedValidator.RuleFor(c => c.Items).ValidItems();
        }

        [RestPostById]
        public ActionResult<QualityProfileResource> Create(QualityProfileResource resource)
        {
            var model = resource.ToModel();
            model = _qualityProfileService.Add(model);
            return Created(model.Id);
        }

        [RestDeleteById]
        public void DeleteProfile(int id)
        {
            _qualityProfileService.Delete(id);
        }

        [RestPutById]
        public ActionResult<QualityProfileResource> Update(QualityProfileResource resource)
        {
            var model = resource.ToModel();

            _qualityProfileService.Update(model);

            return Accepted(model.Id);
        }

        public override QualityProfileResource GetResourceById(int id)
        {
            return _qualityProfileService.Get(id).ToResource();
        }

        [HttpGet]
        public List<QualityProfileResource> GetAll()
        {
            return _qualityProfileService.All().ToResource();
        }
    }
}
