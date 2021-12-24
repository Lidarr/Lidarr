﻿using Lidarr.Http.REST;
using NzbDrone.Core.Configuration;

namespace Lidarr.Api.V1.Config
{
    public class DownloadClientConfigResource : RestResource
    {
        public string DownloadClientWorkingFolders { get; set; }
        public bool EnableCompletedDownloadHandling { get; set; }
        public bool AutoRedownloadFailed { get; set; }
    }

    public static class DownloadClientConfigResourceMapper
    {
        public static DownloadClientConfigResource ToResource(IConfigService model)
        {
            return new DownloadClientConfigResource
            {
                DownloadClientWorkingFolders = model.DownloadClientWorkingFolders,

                EnableCompletedDownloadHandling = model.EnableCompletedDownloadHandling,
                AutoRedownloadFailed = model.AutoRedownloadFailed
            };
        }
    }
}
