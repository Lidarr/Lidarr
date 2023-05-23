using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Qualities;

namespace Lidarr.Api.V1.Profiles.Quality
{
    [V1ApiController("qualityprofile/schema")]
    public class QualityProfileSchemaController : Controller
    {
        private readonly IQualityProfileService _profileService;

        public QualityProfileSchemaController(IQualityProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public QualityProfileResource GetSchema()
        {
            var qualityProfile = _profileService.GetDefaultProfile(string.Empty);

            return qualityProfile.ToResource();
        }
    }
}
