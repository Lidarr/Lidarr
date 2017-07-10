﻿using NzbDrone.Api.REST;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Api.Config
{
    public class MediaManagementConfigResource : RestResource
    {
        public bool AutoUnmonitorPreviouslyDownloadedTracks { get; set; }
        public string RecycleBin { get; set; }
        public bool AutoDownloadPropers { get; set; }
        public bool CreateEmptyArtistFolders { get; set; }
        public FileDateType FileDate { get; set; }

        public bool SetPermissionsLinux { get; set; }
        public string FileChmod { get; set; }
        public string FolderChmod { get; set; }
        public string ChownUser { get; set; }
        public string ChownGroup { get; set; }

        public bool SkipFreeSpaceCheckWhenImporting { get; set; }
        public bool CopyUsingHardlinks { get; set; }
        public bool ImportExtraFiles { get; set; }
        public string ExtraFileExtensions { get; set; }
        public bool EnableMediaInfo { get; set; }
    }

    public static class MediaManagementConfigResourceMapper
    {
        public static MediaManagementConfigResource ToResource(IConfigService model)
        {
            return new MediaManagementConfigResource
            {
                AutoUnmonitorPreviouslyDownloadedTracks = model.AutoUnmonitorPreviouslyDownloadedTracks,
                RecycleBin = model.RecycleBin,
                AutoDownloadPropers = model.AutoDownloadPropers,
                CreateEmptyArtistFolders = model.CreateEmptyArtistFolders,
                FileDate = model.FileDate,

                SetPermissionsLinux = model.SetPermissionsLinux,
                FileChmod = model.FileChmod,
                FolderChmod = model.FolderChmod,
                ChownUser = model.ChownUser,
                ChownGroup = model.ChownGroup,

                SkipFreeSpaceCheckWhenImporting = model.SkipFreeSpaceCheckWhenImporting,
                CopyUsingHardlinks = model.CopyUsingHardlinks,
                ImportExtraFiles = model.ImportExtraFiles,
                ExtraFileExtensions = model.ExtraFileExtensions,
                EnableMediaInfo = model.EnableMediaInfo
            };
        }
    }
}
