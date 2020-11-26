using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Deemix
{
    public class Deemix : DownloadClientBase<DeemixSettings>
    {
        private readonly IDeemixProxyManager _proxyManager;

        public Deemix(IConfigService configService,
                      IDiskProvider diskProvider,
                      IRemotePathMappingService remotePathMappingService,
                      IDeemixProxyManager proxyManager,
                      Logger logger)
            : base(configService, diskProvider, remotePathMappingService, logger)
        {
            _proxyManager = proxyManager;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Deemix;

        public override string Name => "Deemix";

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var proxy = _proxyManager.GetProxy(Settings);

            var queue = proxy.GetQueue();

            foreach (var item in queue)
            {
                item.DownloadClient = Definition.Name;
                item.OutputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host, item.OutputPath);
            }

            return queue;
        }

        public override void RemoveItem(string downloadId, bool deleteData)
        {
            var proxy = _proxyManager.GetProxy(Settings);

            if (deleteData)
            {
                DeleteItemData(downloadId);
            }

            proxy.RemoveFromQueue(downloadId);
        }

        public override string Download(RemoteAlbum remoteAlbum)
        {
            var proxy = _proxyManager.GetProxy(Settings);

            var release = remoteAlbum.Release;

            int bitrate;

            if (release.Codec == "FLAC")
            {
                bitrate = 9;
            }
            else if (release.Container == "320")
            {
                bitrate = 3;
            }
            else
            {
                bitrate = 1;
            }

            return proxy.Download(release.DownloadUrl, bitrate);
        }

        public override DownloadClientInfo GetStatus()
        {
            var proxy = _proxyManager.GetProxy(Settings);
            var config = proxy.GetSettings();

            return new DownloadClientInfo
            {
                IsLocalhost = Settings.Host == "127.0.0.1" || Settings.Host == "localhost",
                OutputRootFolders = new List<OsPath> { _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(config.DownloadLocation)) }
            };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestSettings());
        }

        private ValidationFailure TestSettings()
        {
            var proxy = _proxyManager.GetProxy(Settings);
            var config = proxy.GetSettings();

            if (!config.CreateAlbumFolder)
            {
                return new NzbDroneValidationFailure(string.Empty, "Deemix must have 'Create Album Folders' enabled")
                {
                    InfoLink = HttpRequestBuilder.BuildBaseUrl(Settings.UseSsl, Settings.Host, Settings.Port, Settings.UrlBase),
                    DetailedDescription = "Deemix must have 'Create Album Folders' enabled, otherwise Lidarr will not be able to import the downloads",
                };
            }

            if (!config.CreateSingleFolder)
            {
                return new NzbDroneValidationFailure(string.Empty, "Deemix must have 'Create folder structure for singles' enabled")
                {
                    InfoLink = HttpRequestBuilder.BuildBaseUrl(Settings.UseSsl, Settings.Host, Settings.Port, Settings.UrlBase),
                    DetailedDescription = "Deemix must have 'Create folder structure for singles' enabled, otherwise Lidarr will not be able to import single downloads",
                };
            }

            if (!config.SaveDownloadQueue)
            {
                return new NzbDroneValidationFailure(string.Empty, "Deemix must have 'Save Download Queue' enabled")
                {
                    InfoLink = HttpRequestBuilder.BuildBaseUrl(Settings.UseSsl, Settings.Host, Settings.Port, Settings.UrlBase),
                    DetailedDescription = "Deemix must have 'Save Download Queue' enabled, otherwise Lidarr won't be able to import if deemix restarts",
                };
            }

            return null;
        }
    }
}
