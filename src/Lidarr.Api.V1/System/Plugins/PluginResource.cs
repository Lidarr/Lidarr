using System;
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
        private static string FormatVersion(Version version)
        {
            if (version == null)
            {
                return string.Empty;
            }

            // Always show 4-part version for UI consistency, handling undefined components
            var major = version.Major;
            var minor = version.Minor;
            var build = version.Build == -1 ? 0 : version.Build;
            var revision = version.Revision == -1 ? 0 : version.Revision;
            return $"{major}.{minor}.{build}.{revision}";
        }

        public static PluginResource ToResource(this IPlugin plugin)
        {
            return new PluginResource
            {
                Name = plugin.Name,
                Owner = plugin.Owner,
                GithubUrl = plugin.GithubUrl,
                InstalledVersion = FormatVersion(plugin.InstalledVersion),
                AvailableVersion = FormatVersion(plugin.AvailableVersion),
                UpdateAvailable = plugin.AvailableVersion > plugin.InstalledVersion
            };
        }

        public static List<PluginResource> ToResource(this IEnumerable<IPlugin> plugins)
        {
            return plugins.Select(ToResource).ToList();
        }
    }
}
