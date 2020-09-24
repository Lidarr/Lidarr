using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lidarr.Http;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Profiles.Releases;

namespace Lidarr.Api.V1.Profiles.Release
{
    public class ReleaseProfileModule : LidarrRestModule<ReleaseProfileResource>
    {
        private readonly IReleaseProfileService _releaseProfileService;
        private readonly IIndexerFactory _indexerFactory;

        public ReleaseProfileModule(IReleaseProfileService releaseProfileService, IIndexerFactory indexerFactory)
        {
            _releaseProfileService = releaseProfileService;
            _indexerFactory = indexerFactory;

            GetResourceById = GetById;
            GetResourceAll = GetAll;
            CreateResource = Create;
            UpdateResource = Update;
            DeleteResource = DeleteById;

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

        private ReleaseProfileResource GetById(int id)
        {
            return _releaseProfileService.Get(id).ToResource();
        }

        private List<ReleaseProfileResource> GetAll()
        {
            return _releaseProfileService.All().ToResource();
        }

        private int Create(ReleaseProfileResource resource)
        {
            return _releaseProfileService.Add(resource.ToModel()).Id;
        }

        private void Update(ReleaseProfileResource resource)
        {
            _releaseProfileService.Update(resource.ToModel());
        }

        private void DeleteById(int id)
        {
            _releaseProfileService.Delete(id);
        }
    }
}
