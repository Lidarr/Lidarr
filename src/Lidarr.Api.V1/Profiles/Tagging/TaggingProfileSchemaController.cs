using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Tagging;

namespace Lidarr.Api.V1.Profiles.Tagging
{
    [V1ApiController("taggingprofile/schema")]
    public class TaggingProfileSchemaController : Controller
    {
        private readonly ITaggingProfileService _taggingProfileService;

        public TaggingProfileSchemaController(ITaggingProfileService taggingProfileService)
        {
            _taggingProfileService = taggingProfileService;
        }

        [HttpGet]
        [Produces("application/json")]
        public TaggingProfileResource GetSchema()
        {
            return _taggingProfileService.GetDefaultProfile().ToResource();
        }
    }
}
