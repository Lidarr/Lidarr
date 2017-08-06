﻿using System.Net;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Configuration;
using NLog;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download
{
    public abstract class UsenetClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;

        protected UsenetClientBase(IHttpClient httpClient,
                                   IConfigService configService,
                                   IDiskProvider diskProvider,
                                   IRemotePathMappingService remotePathMappingService,
                                   Logger logger)
            : base(configService, diskProvider, remotePathMappingService, logger)
        {
            _httpClient = httpClient;
        }
        
        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        protected abstract string AddFromNzbFile(RemoteAlbum remoteAlbum, string filename, byte[] fileContent);

        public override string Download(RemoteAlbum remoteAlbum)
        {
            var url = remoteAlbum.Release.DownloadUrl;
            var filename =  FileNameBuilder.CleanFileName(remoteAlbum.Release.Title) + ".nzb";

            byte[] nzbData;

            try
            {
                nzbData = _httpClient.Get(new HttpRequest(url)).ResponseData;

                _logger.Debug("Downloaded nzb for release '{0}' finished ({1} bytes from {2})", remoteAlbum.Release.Title, nzbData.Length, url);
            }
            catch (HttpException ex)
            {
                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", url);
                }
                else
                {
                    _logger.Error(ex, "Downloading nzb for release '{0}' failed ({1})", remoteAlbum.Release.Title, url);
                }

                throw new ReleaseDownloadException(remoteAlbum.Release, "Downloading nzb failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading nzb for release '{0}' failed ({1})", remoteAlbum.Release.Title, url);

                throw new ReleaseDownloadException(remoteAlbum.Release, "Downloading nzb failed", ex);
            }

            _logger.Info("Adding report [{0}] to the queue.", remoteAlbum.Release.Title);
            return AddFromNzbFile(remoteAlbum, filename, nzbData);
        }
    }
}
