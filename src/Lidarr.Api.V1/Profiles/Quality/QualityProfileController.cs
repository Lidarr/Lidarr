using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Profiles.Qualities;

namespace Lidarr.Api.V1.Profiles.Quality
{
    [V1ApiController]
    public class QualityProfileController : RestController<QualityProfileResource>
    {
        private readonly IQualityProfileService _qualityProfileService;
        private readonly ICustomFormatService _formatService;

        public QualityProfileController(IQualityProfileService qualityProfileService, ICustomFormatService formatService)
        {
            _qualityProfileService = qualityProfileService;
            _formatService = formatService;
            SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Cutoff).ValidCutoff();
            SharedValidator.RuleFor(c => c.Items).ValidItems();

            SharedValidator.RuleFor(c => c.FormatItems).Must(items =>
            {
                var all = _formatService.All().Select(f => f.Id).ToList();
                var ids = items.Select(i => i.Format);

                return all.Except(ids).Empty();
            }).WithMessage("All Custom Formats and no extra ones need to be present inside your Profile! Try refreshing your browser.");

            SharedValidator.RuleFor(c => c).Custom((profile, context) =>
            {
                if (profile.FormatItems.Where(x => x.Score > 0).Sum(x => x.Score) < profile.MinFormatScore &&
                    profile.FormatItems.Max(x => x.Score) < profile.MinFormatScore)
                {
                    context.AddFailure("Minimum Custom Format Score can never be satisfied");
                }
            });
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
