using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;

namespace Lidarr.Api.V1.FileSystem
{
    [V1ApiController]
    public class FileSystemController : Controller
    {
        private readonly IFileSystemLookupService _fileSystemLookupService;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskScanService _diskScanService;

        public FileSystemController(IFileSystemLookupService fileSystemLookupService,
                                IDiskProvider diskProvider,
                                IDiskScanService diskScanService)
        {
            _fileSystemLookupService = fileSystemLookupService;
            _diskProvider = diskProvider;
            _diskScanService = diskScanService;
        }

        [HttpGet]
        public IActionResult GetContents(string path, bool includeFiles = false, bool allowFoldersWithoutTrailingSlashes = false)
        {
            return Ok(_fileSystemLookupService.LookupContents(path, includeFiles, allowFoldersWithoutTrailingSlashes));
        }

        [HttpGet("type")]
        public object GetEntityType(string path)
        {
            if (_diskProvider.FileExists(path))
            {
                return new { type = "file" };
            }

            //Return folder even if it doesn't exist on disk to avoid leaking anything from the UI about the underlying system
            return new { type = "folder" };
        }

        [HttpGet("mediafiles")]
        public object GetMediaFiles(string path)
        {
            if (!_diskProvider.FolderExists(path))
            {
                return new string[0];
            }

            return _diskScanService.GetAudioFiles(path).Select(f => new
            {
                Path = f.FullName,
                Name = f.Name
            });
        }
    }
}
