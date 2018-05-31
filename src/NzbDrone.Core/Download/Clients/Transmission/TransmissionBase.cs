using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Transmission
{
    public abstract class TransmissionBase : TorrentClientBase<TransmissionSettings>
    {
        protected readonly ITransmissionProxy _proxy;

        public TransmissionBase(ITransmissionProxy proxy,
            ITorrentFileInfoReader torrentFileInfoReader,
            IHttpClient httpClient,
            IConfigService configService,
            IDiskProvider diskProvider,
            IRemotePathMappingService remotePathMappingService,
            Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var torrents = _proxy.GetTorrents(Settings);

            var items = new List<DownloadClientItem>();

            foreach (var torrent in torrents)
            {
                // If totalsize == 0 the torrent is a magnet downloading metadata
                if (torrent.TotalSize == 0) continue;

                var outputPath = new OsPath(torrent.DownloadDir);

                if (Settings.TvDirectory.IsNotNullOrWhiteSpace())
                {
                    if (!new OsPath(Settings.TvDirectory).Contains(outputPath)) continue;
                }
                else if (Settings.MusicCategory.IsNotNullOrWhiteSpace())
                {
                    var directories = outputPath.FullPath.Split('\\', '/');
                    if (!directories.Contains(Settings.MusicCategory)) continue;
                }

                outputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host, outputPath);

                var item = new DownloadClientItem();
                item.DownloadId = torrent.HashString.ToUpper();
                item.Category = Settings.MusicCategory;
                item.Title = torrent.Name;

                item.DownloadClient = Definition.Name;

                item.OutputPath = GetOutputPath(outputPath, torrent);
                item.TotalSize = torrent.TotalSize;
                item.RemainingSize = torrent.LeftUntilDone;
                item.SeedRatio = torrent.DownloadedEver <= 0 ? 0 :
                    (double)torrent.UploadedEver / torrent.DownloadedEver;

                if (torrent.Eta >= 0)
                {
                    item.RemainingTime = TimeSpan.FromSeconds(torrent.Eta);
                }

                if (!torrent.ErrorString.IsNullOrWhiteSpace())
                {
                    item.Status = DownloadItemStatus.Warning;
                    item.Message = torrent.ErrorString;
                }
                else if (torrent.LeftUntilDone == 0 && (torrent.Status == TransmissionTorrentStatus.Stopped ||
                                                        torrent.Status == TransmissionTorrentStatus.Seeding ||
                                                        torrent.Status == TransmissionTorrentStatus.SeedingWait))
                {
                    item.Status = DownloadItemStatus.Completed;
                }
                else if (torrent.IsFinished && torrent.Status != TransmissionTorrentStatus.Check &&
                         torrent.Status != TransmissionTorrentStatus.CheckWait)
                {
                    item.Status = DownloadItemStatus.Completed;
                }
                else if (torrent.Status == TransmissionTorrentStatus.Queued)
                {
                    item.Status = DownloadItemStatus.Queued;
                }
                else
                {
                    item.Status = DownloadItemStatus.Downloading;
                }

                item.CanMoveFiles = item.CanBeRemoved =
                    torrent.Status == TransmissionTorrentStatus.Stopped &&
                    item.SeedRatio >= torrent.SeedRatioLimit;

                items.Add(item);
            }

            return items;
        }

        public override void RemoveItem(string downloadId, bool deleteData)
        {
            _proxy.RemoveTorrent(downloadId.ToLower(), deleteData, Settings);
        }

        public override DownloadClientInfo GetStatus()
        {
            var config = _proxy.GetConfig(Settings);
            var destDir = config.GetValueOrDefault("download-dir") as string;
            
            if (Settings.MusicCategory.IsNotNullOrWhiteSpace())
            {
                destDir = string.Format("{0}/.{1}", destDir, Settings.MusicCategory);
            }

            return new DownloadClientInfo
            {
                IsLocalhost = Settings.Host == "127.0.0.1" || Settings.Host == "localhost",
                OutputRootFolders = new List<OsPath> { _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(destDir)) }
            };
        }

        protected override string AddFromMagnetLink(RemoteAlbum remoteAlbum, string hash, string magnetLink)
        {
            _proxy.AddTorrentFromUrl(magnetLink, GetDownloadDirectory(), Settings);
            _proxy.SetTorrentSeedingConfiguration(hash, remoteAlbum.SeedConfiguration, Settings);

            var isRecentAlbum = remoteAlbum.IsRecentAlbum();

            if (isRecentAlbum && Settings.RecentTvPriority == (int)TransmissionPriority.First ||
                !isRecentAlbum && Settings.OlderTvPriority == (int)TransmissionPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            return hash;
        }

        protected override string AddFromTorrentFile(RemoteAlbum remoteAlbum, string hash, string filename, byte[] fileContent)
        {
            _proxy.AddTorrentFromData(fileContent, GetDownloadDirectory(), Settings);
            _proxy.SetTorrentSeedingConfiguration(hash, remoteAlbum.SeedConfiguration, Settings);

            var isRecentAlbum = remoteAlbum.IsRecentAlbum();

            if (isRecentAlbum && Settings.RecentTvPriority == (int)TransmissionPriority.First ||
                !isRecentAlbum && Settings.OlderTvPriority == (int)TransmissionPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            return hash;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.Any()) return;
            failures.AddIfNotNull(TestGetTorrents());
        }

        protected virtual OsPath GetOutputPath(OsPath outputPath, TransmissionTorrent torrent)
        {
            return outputPath + torrent.Name;
        }

        protected string GetDownloadDirectory()
        {
            if (Settings.TvDirectory.IsNotNullOrWhiteSpace())
            {
                return Settings.TvDirectory;
            }

            if (!Settings.MusicCategory.IsNotNullOrWhiteSpace())
            {
                return null;
            }

            var config = _proxy.GetConfig(Settings);
            var destDir = (string)config.GetValueOrDefault("download-dir");

            return $"{destDir.TrimEnd('/')}/{Settings.MusicCategory}";
        }

        protected ValidationFailure TestConnection()
        {
            try
            {
                return ValidateVersion();
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure("Username", "Authentication failure")
                {
                    DetailedDescription = string.Format("Please verify your username and password. Also verify if the host running Lidarr isn't blocked from accessing {0} by WhiteList limitations in the {0} configuration.", Name)
                };
            }
            catch (DownloadClientUnavailableException ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure("Host", "Unable to connect")
                {
                    DetailedDescription = "Please verify the hostname and port."
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test");
                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }
        }

        protected abstract ValidationFailure ValidateVersion();

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get torrents");
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }
    }
}
