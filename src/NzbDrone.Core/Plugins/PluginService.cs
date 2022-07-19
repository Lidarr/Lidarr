using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Plugins.Resources;

namespace NzbDrone.Core.Plugins
{
    public interface IPluginService
    {
        (string, string) ParseUrl(string repoUrl);
        RemotePlugin GetRemotePlugin(string repoUrl);
        List<IPlugin> GetInstalledPlugins();
    }

    public class PluginService : IPluginService
    {
        private static readonly Regex RepoRegex = new Regex(@"https://github.com/(?<owner>[^/]*)/(?<name>[^/]*)", RegexOptions.Compiled);
        private static readonly Regex MinVersionRegex = new Regex(@"Minimum Lidarr Version: (?<version>\d+\.\d+\.\d+\.\d+)", RegexOptions.Compiled);

        private readonly IHttpClient _httpClient;
        private readonly List<IPlugin> _installedPlugins;
        private readonly Logger _logger;

        public PluginService(IHttpClient httpClient,
                             IEnumerable<IPlugin> installedPlugins,
                             Logger logger)
        {
            _httpClient = httpClient;
            _installedPlugins = installedPlugins.ToList();
            _logger = logger;
        }

        public (string, string) ParseUrl(string repoUrl)
        {
            var match = RepoRegex.Match(repoUrl);

            if (!match.Success)
            {
                _logger.Warn("Invalid plugin repo URL");
                return (null, null);
            }

            var owner = match.Groups["owner"].Value;
            var name = match.Groups["name"].Value;

            return (owner, name);
        }

        public RemotePlugin GetRemotePlugin(string repoUrl)
        {
            var (owner, name) = ParseUrl(repoUrl);
            var releaseUrl = $"https://api.github.com/repos/{owner}/{name}/releases";

            var releases = _httpClient.Get<List<Release>>(new HttpRequest(releaseUrl)).Resource;

            if (!releases?.Any() ?? true)
            {
                _logger.Warn($"No releases found for {name}");
                return null;
            }

            var latest = releases.OrderByDescending(x => x.PublishedAt).FirstOrDefault(x => IsSupported(x));

            if (latest == null)
            {
                _logger.Warn($"Plugin {name} requires newer version of Lidarr");
                return null;
            }

            var version = Version.Parse(latest.TagName.TrimStart('v'));
            var framework = "net6.0";
            var asset = latest.Assets.FirstOrDefault(x => x.Name.EndsWith($"{framework}.zip"));

            if (asset == null)
            {
                _logger.Warn($"No plugin package found for {framework} for {name}");
                return null;
            }

            return new RemotePlugin
            {
                GithubUrl = repoUrl,
                Name = name,
                Owner = owner,
                Version = version,
                PackageUrl = asset.BrowserDownloadUrl
            };
        }

        public List<IPlugin> GetInstalledPlugins()
        {
            foreach (var plugin in _installedPlugins)
            {
                var remote = GetRemotePlugin(plugin.GithubUrl);
                plugin.AvailableVersion = remote.Version;
            }

            return _installedPlugins;
        }

        private bool IsSupported(Release release)
        {
            var match = MinVersionRegex.Match(release.Body);
            if (match.Success)
            {
                var minVersion = Version.Parse(match.Groups["version"].Value);
                return minVersion <= BuildInfo.Version;
            }

            return true;
        }
    }
}
