using System.IO;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Lifecycle;
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
        private readonly ILifecycleService _lifecycleService;
        private readonly Logger _logger;

        public InstallPluginService(IPluginService pluginService,
                                    IDiskProvider diskProvider,
                                    IAppFolderInfo appFolderInfo,
                                    IHttpClient httpClient,
                                    IArchiveService archiveService,
                                    ILifecycleService lifecycleService,
                                    Logger logger)
        {
            _pluginService = pluginService;
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
            _httpClient = httpClient;
            _archiveService = archiveService;
            _lifecycleService = lifecycleService;
            _logger = logger;
        }

        public void Execute(UninstallPluginCommand message)
        {
            var (owner, name) = _pluginService.ParseUrl(message.GithubUrl);
            UninstallPlugin(owner, name);
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

            _logger.ProgressInfo($"Downloading plugin {package.Name}");
            _httpClient.DownloadFile(package.PackageUrl, packageDestination);

            _logger.ProgressInfo("Extracting Plugin package");
            _archiveService.Extract(packageDestination, Path.Combine(PluginFolder(), package.Owner, package.Name));
            _logger.ProgressInfo($"Installed {package.Name}, restarting");

            Task.Factory.StartNew(() => _lifecycleService.Restart());
        }

        private void UninstallPlugin(string owner, string name)
        {
            _logger.ProgressInfo($"Uninstalling Plugin {owner}/{name}");
            _diskProvider.DeleteFolder(Path.Combine(PluginFolder(), owner, name), true);
            _logger.ProgressInfo($"Uninstalled Plugin {owner}/{name}, restarting");

            Task.Factory.StartNew(() => _lifecycleService.Restart());
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
