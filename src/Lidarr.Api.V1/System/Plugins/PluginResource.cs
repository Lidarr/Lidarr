using System.Collections.Generic;
using System.Linq;
using Lidarr.Http.REST;
using NzbDrone.Core.Plugins;

namespace Lidarr.Api.V1.System.Plugins
{
    public class PluginResource : RestResource
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public string GithubUrl { get; set; }
        public string InstalledVersion { get; set; }
        public string AvailableVersion { get; set; }
        public bool UpdateAvailable { get; set; }
    }

    public static class PluginResourceMapper
    {
        public static PluginResource ToResource(this IPlugin plugin)
        {
            return new PluginResource
            {
                Name = plugin.Name,
                Owner = plugin.Owner,
                GithubUrl = plugin.GithubUrl,
                InstalledVersion = plugin.InstalledVersion.ToString(),
                AvailableVersion = plugin.AvailableVersion.ToString(),
                UpdateAvailable = plugin.AvailableVersion > plugin.InstalledVersion
            };
        }

        public static List<PluginResource> ToResource(this IEnumerable<IPlugin> plugins)
        {
            return plugins.Select(ToResource).ToList();
        }
    }
}
