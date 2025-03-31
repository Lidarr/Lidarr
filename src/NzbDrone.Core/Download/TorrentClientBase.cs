using System;
using System.Net;
using System.Threading.Tasks;
using MonoTorrent;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public abstract class TorrentClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;
        private readonly IBlocklistService _blocklistService;
        protected readonly ITorrentFileInfoReader _torrentFileInfoReader;

        protected TorrentClientBase(ITorrentFileInfoReader torrentFileInfoReader,
            IHttpClient httpClient,
            IConfigService configService,
            IDiskProvider diskProvider,
            IRemotePathMappingService remotePathMappingService,
            ILocalizationService localizationService,
            IBlocklistService blocklistService,
            Logger logger)
            : base(configService, diskProvider, remotePathMappingService, localizationService, logger)
        {
            _httpClient = httpClient;
            _blocklistService = blocklistService;
            _torrentFileInfoReader = torrentFileInfoReader;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public virtual bool PreferTorrentFile => false;

        protected abstract string AddFromMagnetLink(RemoteAlbum remoteAlbum, string hash, string magnetLink);
        protected abstract string AddFromTorrentFile(RemoteAlbum remoteAlbum, string hash, string filename, byte[] fileContent);

        public override async Task<string> Download(RemoteAlbum remoteAlbum, IIndexer indexer)
        {
            var torrentInfo = remoteAlbum.Release as TorrentInfo;

            string magnetUrl = null;
            string torrentUrl = null;

            if (remoteAlbum.Release.DownloadUrl.IsNotNullOrWhiteSpace() && remoteAlbum.Release.DownloadUrl.StartsWith("magnet:"))
            {
                magnetUrl = remoteAlbum.Release.DownloadUrl;
            }
            else
            {
                torrentUrl = remoteAlbum.Release.DownloadUrl;
            }

            if (torrentInfo != null && !torrentInfo.MagnetUrl.IsNullOrWhiteSpace())
            {
                magnetUrl = torrentInfo.MagnetUrl;
            }

            if (PreferTorrentFile)
            {
                if (torrentUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return await DownloadFromWebUrl(remoteAlbum, indexer, torrentUrl);
                    }
                    catch (Exception ex)
                    {
                        if (!magnetUrl.IsNullOrWhiteSpace())
                        {
                            throw;
                        }

                        _logger.Debug("Torrent download failed, trying magnet. ({0})", ex.Message);
                    }
                }

                if (magnetUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return DownloadFromMagnetUrl(remoteAlbum, indexer, magnetUrl);
                    }
                    catch (NotSupportedException ex)
                    {
                        throw new ReleaseDownloadException(remoteAlbum.Release, "Magnet not supported by download client. ({0})", ex.Message);
                    }
                }
            }
            else
            {
                if (magnetUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return DownloadFromMagnetUrl(remoteAlbum, indexer, magnetUrl);
                    }
                    catch (NotSupportedException ex)
                    {
                        if (torrentUrl.IsNullOrWhiteSpace())
                        {
                            throw new ReleaseDownloadException(remoteAlbum.Release, "Magnet not supported by download client. ({0})", ex.Message);
                        }

                        _logger.Debug("Magnet not supported by download client, trying torrent. ({0})", ex.Message);
                    }
                }

                if (torrentUrl.IsNotNullOrWhiteSpace())
                {
                    return await DownloadFromWebUrl(remoteAlbum, indexer, torrentUrl);
                }
            }

            return null;
        }

        private async Task<string> DownloadFromWebUrl(RemoteAlbum remoteAlbum, IIndexer indexer, string torrentUrl)
        {
            byte[] torrentFile = null;

            try
            {
                var request = indexer?.GetDownloadRequest(torrentUrl) ?? new HttpRequest(torrentUrl);
                request.RateLimitKey = remoteAlbum?.Release?.IndexerId.ToString();
                request.Headers.Accept = "application/x-bittorrent";
                request.AllowAutoRedirect = false;

                var response = await RetryStrategy
                    .ExecuteAsync(static async (state, _) => await state._httpClient.GetAsync(state.request), (_httpClient, request))
                    .ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.MovedPermanently ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.SeeOther)
                {
                    var locationHeader = response.Headers.GetSingleValue("Location");

                    _logger.Trace("Torrent request is being redirected to: {0}", locationHeader);

                    if (locationHeader != null)
                    {
                        if (locationHeader.StartsWith("magnet:"))
                        {
                            return DownloadFromMagnetUrl(remoteAlbum, indexer, locationHeader);
                        }

                        request.Url += new HttpUri(locationHeader);

                        return await DownloadFromWebUrl(remoteAlbum, indexer, request.Url.ToString());
                    }

                    throw new WebException("Remote website tried to redirect without providing a location.");
                }

                torrentFile = response.ResponseData;

                _logger.Debug("Downloading torrent for release '{0}' finished ({1} bytes from {2})", remoteAlbum.Release.Title, torrentFile.Length, torrentUrl);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    _logger.Error(ex, "Downloading torrent file for album '{0}' failed since it no longer exists ({1})", remoteAlbum.Release.Title, torrentUrl);
                    throw new ReleaseUnavailableException(remoteAlbum.Release, "Downloading torrent failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", torrentUrl);
                }
                else
                {
                    _logger.Error(ex, "Downloading torrent file for release '{0}' failed ({1})", remoteAlbum.Release.Title, torrentUrl);
                }

                throw new ReleaseDownloadException(remoteAlbum.Release, "Downloading torrent failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading torrent file for release '{0}' failed ({1})", remoteAlbum.Release.Title, torrentUrl);

                throw new ReleaseDownloadException(remoteAlbum.Release, "Downloading torrent failed", ex);
            }

            var filename = string.Format("{0}.torrent", FileNameBuilder.CleanFileName(remoteAlbum.Release.Title));
            var hash = _torrentFileInfoReader.GetHashFromTorrentFile(torrentFile);

            EnsureReleaseIsNotBlocklisted(remoteAlbum, indexer, hash);

            var actualHash = AddFromTorrentFile(remoteAlbum, hash, filename, torrentFile);

            if (actualHash.IsNotNullOrWhiteSpace() && hash != actualHash)
            {
                _logger.Debug(
                    "{0} did not return the expected InfoHash for '{1}', Lidarr could potentially lose track of the download in progress.",
                    Definition.Implementation,
                    remoteAlbum.Release.DownloadUrl);
            }

            return actualHash;
        }

        private string DownloadFromMagnetUrl(RemoteAlbum remoteAlbum, IIndexer indexer, string magnetUrl)
        {
            string hash = null;
            string actualHash = null;

            try
            {
                hash = MagnetLink.Parse(magnetUrl).InfoHash.ToHex();
            }
            catch (FormatException ex)
            {
                throw new ReleaseDownloadException(remoteAlbum.Release, "Failed to parse magnetlink for release '{0}': '{1}'", ex, remoteAlbum.Release.Title, magnetUrl);
            }

            if (hash != null)
            {
                EnsureReleaseIsNotBlocklisted(remoteAlbum, indexer, hash);

                actualHash = AddFromMagnetLink(remoteAlbum, hash, magnetUrl);
            }

            if (actualHash.IsNotNullOrWhiteSpace() && hash != actualHash)
            {
                _logger.Debug(
                    "{0} did not return the expected InfoHash for '{1}', Lidarr could potentially lose track of the download in progress.",
                    Definition.Implementation,
                    remoteAlbum.Release.DownloadUrl);
            }

            return actualHash;
        }

        private void EnsureReleaseIsNotBlocklisted(RemoteAlbum remoteAlbum, IIndexer indexer, string hash)
        {
            var indexerSettings = indexer?.Definition?.Settings as ITorrentIndexerSettings;
            var torrentInfo = remoteAlbum.Release as TorrentInfo;
            var torrentInfoHash = torrentInfo?.InfoHash;

            // If the release didn't come from an interactive search,
            // the hash wasn't known during processing and the
            // indexer is configured to reject blocklisted releases
            // during grab check if it's already been blocklisted.

            if (torrentInfo != null && torrentInfoHash.IsNullOrWhiteSpace())
            {
                // If the hash isn't known from parsing we set it here so it can be used for blocklisting.
                torrentInfo.InfoHash = hash;

                if (remoteAlbum.ReleaseSource != ReleaseSourceType.InteractiveSearch &&
                    indexerSettings?.RejectBlocklistedTorrentHashesWhileGrabbing == true &&
                    _blocklistService.BlocklistedTorrentHash(remoteAlbum.Artist.Id, hash))
                {
                    throw new ReleaseBlockedException(remoteAlbum.Release, "Release previously added to blocklist");
                }
            }
        }
    }
}
