using System.Collections.Generic;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation.Paths;

namespace Lidarr.Api.V1.RemotePathMappings
{
    [V1ApiController]
    public class RemotePathMappingController : RestController<RemotePathMappingResource>
    {
        private readonly IRemotePathMappingService _remotePathMappingService;

        public RemotePathMappingController(IRemotePathMappingService remotePathMappingService,
                                       PathExistsValidator pathExistsValidator,
                                       MappedNetworkDriveValidator mappedNetworkDriveValidator)
        {
            _remotePathMappingService = remotePathMappingService;

            SharedValidator.RuleFor(c => c.Host)
                           .NotEmpty();

            // We cannot use IsValidPath here, because it's a remote path, possibly other OS.
            SharedValidator.RuleFor(c => c.RemotePath)
                           .NotEmpty();

            SharedValidator.RuleFor(c => c.LocalPath)
                .Cascade(CascadeMode.Stop)
                .IsValidPath()
                .SetValidator(mappedNetworkDriveValidator)
                .SetValidator(pathExistsValidator)
                .SetValidator(new SystemFolderValidator())
                .NotEqual("/")
                .WithMessage("Cannot be set to '/'");
        }

        public override RemotePathMappingResource GetResourceById(int id)
        {
            return _remotePathMappingService.Get(id).ToResource();
        }

        [RestPostById]
        [Consumes("application/json")]
        public ActionResult<RemotePathMappingResource> CreateMapping([FromBody] RemotePathMappingResource resource)
        {
            var model = resource.ToModel();

            return Created(_remotePathMappingService.Add(model).Id);
        }

        [HttpGet]
        [Produces("application/json")]
        public List<RemotePathMappingResource> GetMappings()
        {
            return _remotePathMappingService.All().ToResource();
        }

        [RestDeleteById]
        public void DeleteMapping(int id)
        {
            _remotePathMappingService.Remove(id);
        }

        [RestPutById]
        public ActionResult<RemotePathMappingResource> UpdateMapping([FromBody] RemotePathMappingResource resource)
        {
            var mapping = resource.ToModel();

            return Accepted(_remotePathMappingService.Update(mapping));
        }
    }
}
