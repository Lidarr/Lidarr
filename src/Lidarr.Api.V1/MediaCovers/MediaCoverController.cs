using System.IO;
using System.Text.RegularExpressions;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace Lidarr.Api.V1.MediaCovers
{
    [V1ApiController]
    public class MediaCoverController : Controller
    {
        private static readonly Regex RegexResizedImage = new Regex(@"-\d+(?=\.(jpg|png|gif)$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly IContentTypeProvider _mimeTypeProvider;

        public MediaCoverController(IAppFolderInfo appFolderInfo, IDiskProvider diskProvider)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _mimeTypeProvider = new FileExtensionContentTypeProvider();
        }

        [HttpGet(@"artist/{artistId:int}/{filename:regex((.+)\.(jpg|png|gif))}")]
        public IActionResult GetArtistMediaCover(int artistId, string filename)
        {
            var filePath = Path.Combine(_appFolderInfo.GetAppDataPath(), "MediaCover", artistId.ToString(), filename);

            if (!_diskProvider.FileExists(filePath) || _diskProvider.GetFileSize(filePath) == 0)
            {
                // Return the full sized image if someone requests a non-existing resized one.
                // TODO: This code can be removed later once everyone had the update for a while.
                var basefilePath = RegexResizedImage.Replace(filePath, "");
                if (basefilePath == filePath || !_diskProvider.FileExists(basefilePath))
                {
                    return NotFound();
                }

                filePath = basefilePath;
            }

            return PhysicalFile(filePath, GetContentType(filePath));
        }

        [HttpGet(@"album/{albumId:int}/{filename:regex((.+)\.(jpg|png|gif))}")]
        public IActionResult GetAlbumMediaCover(int albumId, string filename)
        {
            var filePath = Path.Combine(_appFolderInfo.GetAppDataPath(), "MediaCover", "Albums", albumId.ToString(), filename);

            if (!_diskProvider.FileExists(filePath) || _diskProvider.GetFileSize(filePath) == 0)
            {
                // Return the full sized image if someone requests a non-existing resized one.
                // TODO: This code can be removed later once everyone had the update for a while.
                var basefilePath = RegexResizedImage.Replace(filePath, "");
                if (basefilePath == filePath || !_diskProvider.FileExists(basefilePath))
                {
                    return NotFound();
                }

                filePath = basefilePath;
            }

            return PhysicalFile(filePath, GetContentType(filePath));
        }

        private string GetContentType(string filePath)
        {
            if (!_mimeTypeProvider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return contentType;
        }
    }
}
