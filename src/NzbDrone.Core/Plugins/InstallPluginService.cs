using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Plugins.Commands;
using NzbDrone.Core.Plugins.Resources;

namespace NzbDrone.Core.Plugins
{
    public class InstallPluginService : IExecute<InstallPluginCommand>
    {
        private static readonly Regex RepoRegex = new Regex(@"https://github.com/(?<repo>[^/]*)/(?<name>[^/]*)", RegexOptions.Compiled);

        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IHttpClient _httpClient;
        private readonly IArchiveService _archiveService;
        private readonly Logger _logger;

        public InstallPluginService(IDiskProvider diskProvider,
                                    IAppFolderInfo appFolderInfo,
                                    IHttpClient httpClient,
                                    IArchiveService archiveService,
                                    Logger logger)
        {
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
            _httpClient = httpClient;
            _archiveService = archiveService;
            _logger = logger;
        }

        public void Execute(InstallPluginCommand message)
        {
            var package = GetPlugin(message.GithubUrl);
            if (package != null)
            {
                InstallPlugin(package);
            }
        }

        private void InstallPlugin(Plugin package)
        {
            EnsurePluginFolder();

            string tempFolder = TempFolder();
            if (_diskProvider.FolderExists(tempFolder))
            {
                _logger.Info("Deleting old plugin packages");
                _diskProvider.DeleteFolder(tempFolder, true);
            }

            var packageDestination = Path.Combine(tempFolder, $"{package.Name}.zip");

            _logger.ProgressInfo($"Downloading plugin {package.Name}");
            _httpClient.DownloadFile(package.PackageUrl, packageDestination);

            _logger.ProgressInfo("Extracting Plugin package");
            _archiveService.Extract(packageDestination, Path.Combine(PluginFolder(), package.Name));
            _logger.ProgressInfo($"Installed {package.Name}");
        }

        private Plugin GetPlugin(string repoUrl)
        {
            var match = RepoRegex.Match(repoUrl);

            if (!match.Success)
            {
                _logger.ProgressInfo("Invalid plugin repo URL");
                return null;
            }

            var repo = match.Groups["repo"].Value;
            var name = match.Groups["name"].Value;

            var releaseUrl = $"https://api.github.com/repos/{repo}/{name}/releases";

            var releases = _httpClient.Get<List<Release>>(new HttpRequest(releaseUrl)).Resource;

            if (!releases?.Any() ?? true)
            {
                _logger.ProgressInfo("No releases found for {name}");
                return null;
            }

            var latest = releases.OrderByDescending(x => x.PublishedAt).First();
            var framework = PlatformInfo.IsNetCore ? "netcoreapp3.1" : "net462";
            var asset = latest.Assets.FirstOrDefault(x => x.Name.EndsWith($"{framework}.zip"));

            if (asset == null)
            {
                _logger.ProgressInfo("No plugin package found for {framework} for {name}");
                return null;
            }

            return new Plugin
            {
                GithubUrl = repoUrl,
                Name = name,
                PackageUrl = asset.BrowserDownloadUrl
            };
        }

        private void EnsurePluginFolder()
        {
            _diskProvider.EnsureFolder(PluginFolder());
        }

        private string PluginFolder()
        {
            return _appFolderInfo.GetPluginPath();
        }

        private string TempFolder()
        {
            return Path.Combine(_appFolderInfo.TempFolder, "plugins");
        }
    }
}
