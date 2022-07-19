using System.Collections.Generic;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Plugins;

namespace Lidarr.Api.V1.System.Plugins
{
    [V1ApiController("system/plugins")]
    public class PluginController : Controller
    {
        private readonly IPluginService _pluginService;

        public PluginController(IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        [HttpGet]
        public List<PluginResource> GetInstalledPlugins()
        {
            return _pluginService.GetInstalledPlugins().ToResource();
        }
    }
}
