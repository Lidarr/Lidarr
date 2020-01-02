﻿using System.Collections.Generic;
using Lidarr.Http;
using NzbDrone.Core.DiskSpace;

namespace Lidarr.Api.V1.DiskSpace
{
    public class DiskSpaceModule : LidarrRestModule<DiskSpaceResource>
    {
        private readonly IDiskSpaceService _diskSpaceService;

        public DiskSpaceModule(IDiskSpaceService diskSpaceService)
            : base("diskspace")
        {
            _diskSpaceService = diskSpaceService;
            GetResourceAll = GetFreeSpace;
        }

        public List<DiskSpaceResource> GetFreeSpace()
        {
            return _diskSpaceService.GetFreeSpace().ConvertAll(DiskSpaceResourceMapper.MapToResource);
        }
    }
}
