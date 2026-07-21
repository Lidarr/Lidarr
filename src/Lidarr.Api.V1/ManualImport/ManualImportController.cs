using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Extensions;
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
        private readonly IManualImportProgressService _manualImportProgressService;
        private readonly Logger _logger;

        public ManualImportController(IManualImportService manualImportService,
                                  IManualImportProgressService manualImportProgressService,
                                  IArtistService artistService,
                                  IAlbumService albumService,
                                  IReleaseService releaseService,
                                  Logger logger)
        {
            _artistService = artistService;
            _albumService = albumService;
            _releaseService = releaseService;
            _manualImportService = manualImportService;
            _manualImportProgressService = manualImportProgressService;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult UpdateItems([FromBody] List<ManualImportUpdateResource> resource)
        {
            return Accepted(UpdateImportItems(resource));
        }

        [HttpGet("progress/{id}")]
        public ActionResult<ManualImportProgress> GetProgress(string id)
        {
            var progress = _manualImportProgressService.Get(id);
            if (progress == null)
            {
                return NotFound();
            }

            return progress;
        }

        [HttpGet]
        public List<ManualImportResource> GetMediaFiles(string folder, string downloadId, int? artistId, bool filterExistingFiles = true, bool replaceExistingFiles = true, string progressId = null)
        {
            if (progressId.IsNotNullOrWhiteSpace())
            {
                _manualImportProgressService.Begin(progressId, "Preparing manual import scan");
            }

            try
            {
                NzbDrone.Core.Music.Artist artist = null;

                if (artistId > 0)
                {
                    artist = _artistService.GetArtist(artistId.Value);
                }

                var filter = filterExistingFiles ? FilterFilesType.Matched : FilterFilesType.None;

                var result = _manualImportService.GetMediaFiles(folder, downloadId, artist, filter, replaceExistingFiles).ToResource().Select(AddQualityWeight).ToList();
                _manualImportProgressService.Complete("Manual import scan complete");
                return result;
            }
            catch
            {
                _manualImportProgressService.Fail("Manual import scan failed");
                throw;
            }
            finally
            {
                _manualImportProgressService.ClearCurrent();
            }
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

        private List<ManualImportResource> UpdateImportItems(List<ManualImportUpdateResource> resources)
        {
            var progressId = resources.Select(x => x.ProgressId).FirstOrDefault(x => x.IsNotNullOrWhiteSpace());
            if (progressId.IsNotNullOrWhiteSpace())
            {
                _manualImportProgressService.Begin(progressId, "Preparing track identification");
            }

            try
            {
                var items = new List<ManualImportItem>();
                foreach (var resource in resources)
                {
                    items.Add(new ManualImportItem
                    {
                        Id = resource.Id,
                        Path = resource.Path,
                        Name = resource.Name,
                        Artist = resource.ArtistId.HasValue ? _artistService.GetArtist(resource.ArtistId.Value) : null,
                        Album = resource.AlbumId.HasValue ? _albumService.GetAlbum(resource.AlbumId.Value) : null,
                        Release = resource.AlbumReleaseId.HasValue ? _releaseService.GetRelease(resource.AlbumReleaseId.Value) : null,
                        Quality = resource.Quality,
                        ReleaseGroup = resource.ReleaseGroup,
                        IndexerFlags = resource.IndexerFlags,
                        DownloadId = resource.DownloadId,
                        AdditionalFile = resource.AdditionalFile,
                        ReplaceExistingFiles = resource.ReplaceExistingFiles,
                        DisableReleaseSwitching = resource.DisableReleaseSwitching
                    });
                }

                var result = _manualImportService.UpdateItems(items).Select(x => x.ToResource()).ToList();
                _manualImportProgressService.Complete("Track identification complete");
                return result;
            }
            catch
            {
                _manualImportProgressService.Fail("Track identification failed");
                throw;
            }
            finally
            {
                _manualImportProgressService.ClearCurrent();
            }
        }
    }
}
