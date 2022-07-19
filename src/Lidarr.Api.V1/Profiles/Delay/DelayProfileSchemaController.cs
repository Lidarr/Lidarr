using Lidarr.Http;
using Lidarr.Http.REST;
using NzbDrone.Core.Profiles.Delay;

namespace Lidarr.Api.V1.Profiles.Delay
{
    [V1ApiController("delayprofile/schema")]
    public class DelayProfileSchemaController : RestController<DelayProfileResource>
    {
        private readonly IDelayProfileService _profileService;

        public DelayProfileSchemaController(IDelayProfileService profileService)
        {
            _profileService = profileService;
        }

        public override DelayProfileResource GetResourceById(int id)
        {
            return _profileService.GetDefaultProfile().ToResource();
        }
    }
}
