using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Profiles.Releases;

namespace Lidarr.Api.V1.Profiles.Release
{
    [V1ApiController]
    public class ReleaseProfileController : RestController<ReleaseProfileResource>
    {
        private readonly IReleaseProfileService _releaseProfileService;
        private readonly IIndexerFactory _indexerFactory;

        public ReleaseProfileController(IReleaseProfileService releaseProfileService, IIndexerFactory indexerFactory)
        {
            _releaseProfileService = releaseProfileService;
            _indexerFactory = indexerFactory;

            SharedValidator.RuleFor(r => r).Custom((restriction, context) =>
            {
                if (restriction.Ignored.IsNullOrWhiteSpace() && restriction.Required.IsNullOrWhiteSpace() && restriction.Preferred.Empty())
                {
                    context.AddFailure("Either 'Must contain' or 'Must not contain' is required");
                }

                if (restriction.Enabled && restriction.IndexerId != 0 && !_indexerFactory.Exists(restriction.IndexerId))
                {
                    context.AddFailure(nameof(ReleaseProfile.IndexerId), "Indexer does not exist");
                }

                if (restriction.Preferred.Any(p => p.Key.IsNullOrWhiteSpace()))
                {
                    context.AddFailure("Preferred", "Term cannot be empty or consist of only spaces");
                }
            });
        }

        public override ReleaseProfileResource GetResourceById(int id)
        {
            return _releaseProfileService.Get(id).ToResource();
        }

        [HttpGet]
        public List<ReleaseProfileResource> GetAll()
        {
            return _releaseProfileService.All().ToResource();
        }

        [RestPostById]
        public ActionResult<ReleaseProfileResource> Create(ReleaseProfileResource resource)
        {
            return Created(_releaseProfileService.Add(resource.ToModel()).Id);
        }

        [RestPutById]
        public ActionResult<ReleaseProfileResource> Update(ReleaseProfileResource resource)
        {
            _releaseProfileService.Update(resource.ToModel());
            return Accepted(resource.Id);
        }

        [RestDeleteById]
        public void DeleteById(int id)
        {
            _releaseProfileService.Delete(id);
        }
    }
}
