using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.TrackImport.Manual;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;

namespace Lidarr.Api.V1.ManualImport
{
    [V1ApiController]
    public class ManualImportController : Controller
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IReleaseService _releaseService;
        private readonly IManualImportService _manualImportService;
        private readonly Logger _logger;

        public ManualImportController(IManualImportService manualImportService,
                                  IArtistService artistService,
                                  IAlbumService albumService,
                                  IReleaseService releaseService,
                                  Logger logger)
        {
            _artistService = artistService;
            _albumService = albumService;
            _releaseService = releaseService;
            _manualImportService = manualImportService;
            _logger = logger;
        }

        [HttpPut]
        public IActionResult UpdateItems(List<ManualImportResource> resource)
        {
            return Accepted(UpdateImportItems(resource));
        }

        [HttpGet]
        public List<ManualImportResource> GetMediaFiles(string folder, string downloadId, int? artistId, bool filterExistingFiles = true, bool replaceExistingFiles = true)
        {
            NzbDrone.Core.Music.Artist artist = null;

            if (artistId > 0)
            {
                artist = _artistService.GetArtist(artistId.Value);
            }

            var filter = filterExistingFiles ? FilterFilesType.Matched : FilterFilesType.None;

            return _manualImportService.GetMediaFiles(folder, downloadId, artist, filter, replaceExistingFiles).ToResource().Select(AddQualityWeight).ToList();
        }

        private ManualImportResource AddQualityWeight(ManualImportResource item)
        {
            if (item.Quality != null)
            {
                item.QualityWeight = Quality.DefaultQualityDefinitions.Single(q => q.Quality == item.Quality.Quality).Weight;
                item.QualityWeight += item.Quality.Revision.Real * 10;
                item.QualityWeight += item.Quality.Revision.Version;
            }

            return item;
        }

        private List<ManualImportResource> UpdateImportItems(List<ManualImportResource> resources)
        {
            var items = new List<ManualImportItem>();
            foreach (var resource in resources)
            {
                items.Add(new ManualImportItem
                {
                    Id = resource.Id,
                    Path = resource.Path,
                    Name = resource.Name,
                    Size = resource.Size,
                    Artist = resource.Artist == null ? null : _artistService.GetArtist(resource.Artist.Id),
                    Album = resource.Album == null ? null : _albumService.GetAlbum(resource.Album.Id),
                    Release = resource.AlbumReleaseId == 0 ? null : _releaseService.GetRelease(resource.AlbumReleaseId),
                    Quality = resource.Quality,
                    DownloadId = resource.DownloadId,
                    AdditionalFile = resource.AdditionalFile,
                    ReplaceExistingFiles = resource.ReplaceExistingFiles,
                    DisableReleaseSwitching = resource.DisableReleaseSwitching
                });
            }

            return _manualImportService.UpdateItems(items).Select(x => x.ToResource()).ToList();
        }
    }
}
