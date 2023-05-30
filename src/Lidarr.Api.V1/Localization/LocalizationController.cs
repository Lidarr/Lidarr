using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Localization;

namespace Lidarr.Api.V1.Localization
{
    [V1ApiController]
    public class LocalizationController : Controller
    {
        private readonly ILocalizationService _localizationService;

        public LocalizationController(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        [HttpGet]
        [Produces("application/json")]
        public LocalizationResource GetLocalizationDictionary()
        {
            return _localizationService.GetLocalizationDictionary().ToResource();
        }
    }
}
