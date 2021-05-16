using System.Collections.Generic;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.RootFolders
{
    [V1ApiController]
    public class RootFolderController : RestControllerWithSignalR<RootFolderResource, RootFolder>
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderController(IRootFolderService rootFolderService,
                                IBroadcastSignalRMessage signalRBroadcaster,
                                RootFolderValidator rootFolderValidator,
                                PathExistsValidator pathExistsValidator,
                                MappedNetworkDriveValidator mappedNetworkDriveValidator,
                                StartupFolderValidator startupFolderValidator,
                                SystemFolderValidator systemFolderValidator,
                                FolderWritableValidator folderWritableValidator,
                                QualityProfileExistsValidator qualityProfileExistsValidator,
                                MetadataProfileExistsValidator metadataProfileExistsValidator)
            : base(signalRBroadcaster)
        {
            _rootFolderService = rootFolderService;

            SharedValidator.RuleFor(c => c.Path)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .IsValidPath()
                .SetValidator(mappedNetworkDriveValidator)
                .SetValidator(startupFolderValidator)
                .SetValidator(pathExistsValidator)
                .SetValidator(systemFolderValidator)
                .SetValidator(folderWritableValidator);

            PostValidator.RuleFor(c => c.Path)
                .SetValidator(rootFolderValidator);

            SharedValidator.RuleFor(c => c.Name)
                .NotEmpty();

            SharedValidator.RuleFor(c => c.DefaultMetadataProfileId)
                .SetValidator(metadataProfileExistsValidator);

            SharedValidator.RuleFor(c => c.DefaultQualityProfileId)
                .SetValidator(qualityProfileExistsValidator);
        }

        public override RootFolderResource GetResourceById(int id)
        {
            return _rootFolderService.Get(id).ToResource();
        }

        [RestPostById]
        public ActionResult<RootFolderResource> CreateRootFolder(RootFolderResource rootFolderResource)
        {
            var model = rootFolderResource.ToModel();

            return Created(_rootFolderService.Add(model).Id);
        }

        [RestPutById]
        public ActionResult<RootFolderResource> UpdateRootFolder(RootFolderResource rootFolderResource)
        {
            var model = rootFolderResource.ToModel();

            if (model.Path != rootFolderResource.Path)
            {
                throw new BadRequestException("Cannot edit root folder path");
            }

            _rootFolderService.Update(model);

            return Accepted(model.Id);
        }

        [HttpGet]
        public List<RootFolderResource> GetRootFolders()
        {
            return _rootFolderService.AllWithSpaceStats().ToResource();
        }

        [RestDeleteById]
        public void DeleteFolder(int id)
        {
            _rootFolderService.Remove(id);
        }
    }
}
