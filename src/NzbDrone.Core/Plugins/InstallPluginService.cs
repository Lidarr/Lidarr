using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Plugins.Commands;

namespace NzbDrone.Core.Plugins
{
    public class InstallPluginService : IExecute<InstallPluginCommand>, IExecute<UninstallPluginCommand>
    {
        private readonly IPluginService _pluginService;
        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IHttpClient _httpClient;
        private readonly IArchiveService _archiveService;
        private readonly Logger _logger;

        public InstallPluginService(IPluginService pluginService,
                                    IDiskProvider diskProvider,
                                    IAppFolderInfo appFolderInfo,
                                    IHttpClient httpClient,
                                    IArchiveService archiveService,
                                    Logger logger)
        {
            _pluginService = pluginService;
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
            _httpClient = httpClient;
            _archiveService = archiveService;
            _logger = logger;
        }

        public void Execute(UninstallPluginCommand message)
        {
            var (owner, name) = _pluginService.ParseUrl(message.GithubUrl);

            // Get installed version before uninstalling
            var installedPlugins = _pluginService.GetInstalledPlugins();
            var installedPlugin = installedPlugins.FirstOrDefault(p => p.Owner == owner && p.Name == name);
            var version = installedPlugin?.InstalledVersion;

            UninstallPlugin(owner, name, version);
        }

        public void Execute(InstallPluginCommand message)
        {
            var package = _pluginService.GetRemotePlugin(message.GithubUrl);
            if (package != null)
            {
                InstallPlugin(package);
            }
        }

        private void InstallPlugin(RemotePlugin package)
        {
            EnsurePluginFolder();

            var tempFolder = TempFolder();
            if (_diskProvider.FolderExists(tempFolder))
            {
                _logger.Info("Deleting old plugin packages");
                _diskProvider.DeleteFolder(tempFolder, true);
            }

            var packageDestination = Path.Combine(tempFolder, $"{package.Name}.zip");
            var packageTitle = $"{package.Owner}/{package.Name} v{package.Version}";
            _logger.ProgressInfo($"Downloading plugin [{packageTitle}]");
            _httpClient.DownloadFile(package.PackageUrl, packageDestination);

            _logger.ProgressInfo($"Extracting plugin [{packageTitle}]");
            _archiveService.Extract(packageDestination, Path.Combine(PluginFolder(), package.Owner, package.Name));
            _logger.ProgressInfo($"Plugin [{package.Owner}/{package.Name}] v{package.Version} installed. Please restart Lidarr.");
        }

        private void UninstallPlugin(string owner, string name, Version version)
        {
            _logger.ProgressInfo($"Uninstalling plugin [{owner}/{name}]");
            var pluginFolder = Path.Combine(PluginFolder(), owner, name);
            _logger.Debug("Deleting folder: {0}", pluginFolder);
            _diskProvider.DeleteFolder(pluginFolder, true);

            if (version != null)
            {
                _logger.ProgressInfo($"Plugin [{owner}/{name}] v{version} uninstalled. Please restart Lidarr.");
            }
            else
            {
                _logger.ProgressInfo($"Plugin [{owner}/{name}] uninstalled. Please restart Lidarr.");
            }
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
