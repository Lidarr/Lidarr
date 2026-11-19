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
        (string Owner, string Name, string Tree) ParseRepositoryInput(string input);
        RemotePlugin GetRemotePlugin(string input);
        List<IPlugin> GetInstalledPlugins();
    }

    public partial class PluginService : IPluginService
    {
        private readonly IHttpClient _httpClient;
        private readonly IPlatformInfo _platformInfo;
        private readonly List<IPlugin> _installedPlugins;
        private readonly Logger _logger;

        public PluginService(
            IHttpClient httpClient,
            IPlatformInfo platformInfo,
            IEnumerable<IPlugin> installedPlugins,
            Logger logger)
        {
            _httpClient = httpClient;
            _platformInfo = platformInfo;
            _logger = logger;
            _installedPlugins = installedPlugins?.ToList() ?? new List<IPlugin>();
        }

        private string Framework => $"net{_platformInfo.Version.Major}.0";

        public RemotePlugin GetRemotePlugin(string input)
        {
            try
            {
                var (owner, name, tree) = ParseRepositoryInput(input);
                if (string.IsNullOrWhiteSpace(owner))
                {
                    return null;
                }

                _logger.Trace($"Fetching releases for {owner}/{name}" + (tree != null ? $" on tree '{tree}'" : ""));

                var releasesUrl = $"https://api.github.com/repos/{owner}/{name}/releases";
                var releases = _httpClient.Get<List<Release>>(new HttpRequest(releasesUrl)).Resource;

                if (releases?.Any() != true)
                {
                    _logger.Warn($"No releases found for {owner}/{name}");
                    return null;
                }

                _logger.Trace($"Found {releases.Count} total releases, filtering for framework {Framework}" + (tree != null ? $" and tree '{tree}'" : ""));

                var compatibleReleases = releases
                    .Where(r => !r.Draft &&
                    (IsMatchingTree(r.TargetCommitish, tree) || IsMatchingTree(r.TagName, tree)) &&
                    HasCompatibleAsset(r, Framework) && MeetsMinimumVersion(r.Body))
                    .OrderByDescending(r => r.PublishedAt)
                    .ToList();

                _logger.Trace($"Found {compatibleReleases.Count} compatible releases after filtering");

                var release = compatibleReleases.FirstOrDefault();
                if (release == null)
                {
                    _logger.Warn($"No compatible release found for {name} with framework {Framework}" +
                        (tree != null ? $" and tree {tree}" : ""));
                    return null;
                }

                var releaseUrl = release.HtmlUrl;
                var urlMatch = RepoUrlExtractorRegex().Match(releaseUrl);
                if (urlMatch.Success)
                {
                    owner = urlMatch.Groups["owner"].Value;
                    name = urlMatch.Groups["n"].Value;
                }

                var targetTree = release.TargetCommitish;
                var isDefaultTree = PluginVersion.IsDefaultTree(targetTree);
                var tag = release.TagName;

                var version = isDefaultTree
                    ? ParseFullVersion(tag)
                    : new PluginVersion(ParseVersionFromTag(tag), targetTree);

                var actualTree = isDefaultTree ? null : targetTree;

                var asset = release.Assets.FirstOrDefault(a =>
                    a.Name.Contains($"{Framework}.zip", StringComparison.OrdinalIgnoreCase));

                if (asset == null)
                {
                    _logger.Warn($"No asset found matching {Framework}.zip for {name}");
                    return null;
                }

                var githubUrl = $"https://github.com/{owner}/{name}" + (actualTree != null ? $"/tree/{actualTree}" : "");

                _logger.Info($"Found plugin {owner}/{name} v{version} from tree '{actualTree ?? "default"}' with asset {asset.Name}");
                return new RemotePlugin
                {
                    GithubUrl = githubUrl,
                    Name = name,
                    Owner = owner,
                    Version = version,
                    PackageUrl = asset.BrowserDownloadUrl,
                    Tree = actualTree
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to get remote plugin for {input}");
                return null;
            }
        }

        public List<IPlugin> GetInstalledPlugins()
        {
            foreach (var plugin in _installedPlugins)
            {
                try
                {
                    var remote = GetRemotePlugin(plugin.GithubUrl);
                    plugin.AvailableVersion = remote?.Version ?? new PluginVersion(new Version(0, 0, 0, 0), "unavailable");
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, $"Unable to check updates for {plugin.Owner}/{plugin.Name}");
                    plugin.AvailableVersion = new PluginVersion(new Version(0, 0, 0, 0), "unavailable");
                }
            }

            return _installedPlugins;
        }

        public (string Owner, string Name, string Tree) ParseRepositoryInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.Error("Repository input cannot be empty");
                return default;
            }

            input = input.Trim();

            string owner = null;
            string name = null;
            string tree = null;

            var urlMatch = GitHubUrlRegex().Match(input);
            if (urlMatch.Success)
            {
                name = urlMatch.Groups["n"].Value;
                return (
                    urlMatch.Groups["owner"].Value,
                    name.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name,
                    urlMatch.Groups["tree"].Success ? urlMatch.Groups["tree"].Value : null);
            }

            var nameTokens = new List<string>();

            foreach (var token in input.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = TagSplitterRegex().Split(token).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

                for (var i = 0; i < parts.Length; i++)
                {
                    switch (parts[i])
                    {
                        case "@" when i + 1 < parts.Length:
                            owner = parts[++i];
                            break;
                        case "#" when i + 1 < parts.Length:
                            tree = parts[++i];
                            break;
                        case not "@" and not "#":
                            nameTokens.Add(parts[i]);
                            break;
                    }
                }
            }

            name = nameTokens.Count > 0 ? string.Concat(nameTokens).Trim() : null;

            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(name))
            {
                return default;
            }

            return (owner, name, tree);
        }

        private static bool IsMatchingTree(string targetTree, string requestedTree) =>
            requestedTree == null
                ? PluginVersion.IsDefaultTree(targetTree)
                : targetTree.Equals(requestedTree, StringComparison.OrdinalIgnoreCase);

        private static bool HasCompatibleAsset(Release release, string framework)
            => release.Assets.Any(a => a.Name.Contains($"{framework}.zip", StringComparison.OrdinalIgnoreCase));

        private static PluginVersion ParseFullVersion(string tag)
        {
            var match = VersionWithPrereleaseRegex().Match(tag.TrimStart('v'));
            return match.Success
                ? new PluginVersion(
                    Version.Parse(match.Groups["version"].Value),
                    match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null)
                : new PluginVersion(ParseVersionFromTag(tag));
        }

        private static Version ParseVersionFromTag(string tag)
        {
            var match = VersionOnlyRegex().Match(tag.TrimStart('v'));
            return match.Success && Version.TryParse(match.Groups[1].Value, out var version)
                ? version
                : new Version(0, 0, 0);
        }

        private bool MeetsMinimumVersion(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return true;
            }

            var match = MinimumVersionRegex().Match(body);
            return !match.Success || Version.Parse(match.Groups["version"].Value) <= BuildInfo.Version;
        }

        [GeneratedRegex(@"^https?://github\.com/(?<owner>[^/]+)/(?<n>[^/\s]+)(?:/tree/(?<tree>[^/\s]+))?(?:\.git)?/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex GitHubUrlRegex();

        [GeneratedRegex(@"([@#])", RegexOptions.Compiled)]
        private static partial Regex TagSplitterRegex();

        [GeneratedRegex(@"github\.com/(?<owner>[^/]+)/(?<n>[^/]+)/", RegexOptions.Compiled)]
        private static partial Regex RepoUrlExtractorRegex();

        [GeneratedRegex(@"Minimum\s+Lidarr\s+Version:?\s*(?:\*\*)?[\s]*(?<version>\d+\.\d+\.\d+\.\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private partial Regex MinimumVersionRegex();

        [GeneratedRegex(@"^(?<version>\d+\.\d+\.\d+(?:\.\d+)?)(?:-(?<prerelease>.+))?$", RegexOptions.Compiled)]
        private static partial Regex VersionWithPrereleaseRegex();

        [GeneratedRegex(@"^(\d+\.\d+\.\d+(?:\.\d+)?)", RegexOptions.Compiled)]
        private static partial Regex VersionOnlyRegex();
    }
}
