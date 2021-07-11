using System.Collections.Generic;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DiskSpace;

namespace Lidarr.Api.V1.DiskSpace
{
    [V1ApiController("diskspace")]
    public class DiskSpaceController : Controller
    {
        private readonly IDiskSpaceService _diskSpaceService;

        public DiskSpaceController(IDiskSpaceService diskSpaceService)
        {
            _diskSpaceService = diskSpaceService;
        }

        [HttpGet]
        public List<DiskSpaceResource> GetFreeSpace()
        {
            return _diskSpaceService.GetFreeSpace().ConvertAll(DiskSpaceResourceMapper.MapToResource);
        }
    }
}
