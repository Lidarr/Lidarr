using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Delay;

namespace Lidarr.Api.V1.Profiles.Delay
{
    [V1ApiController("delayprofile/schema")]
    public class DelayProfileSchemaController : Controller
    {
        private readonly IDelayProfileService _delayProfileService;

        public DelayProfileSchemaController(IDelayProfileService delayProfileService)
        {
            _delayProfileService = delayProfileService;
        }

        [HttpGet]
        [Produces("application/json")]
        public DelayProfileResource GetSchema()
        {
            return _delayProfileService.GetDefaultProfile().ToResource();
        }
    }
}
