using System.Threading.Tasks;
using Lidarr.Http;
using Lidarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;

namespace Lidarr.Api.V1.Queue
{
    [V1ApiController("queue")]
    public class QueueActionController : Controller
    {
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly IDownloadService _downloadService;

        public QueueActionController(IPendingReleaseService pendingReleaseService,
                                     IDownloadService downloadService)
        {
            _pendingReleaseService = pendingReleaseService;
            _downloadService = downloadService;
        }

        [HttpPost("grab/{id:int}")]
        public async Task<object> Grab(int id)
        {
            var pendingRelease = _pendingReleaseService.FindPendingQueueItem(id);

            if (pendingRelease == null)
            {
                throw new NotFoundException();
            }

            await _downloadService.DownloadReport(pendingRelease.RemoteAlbum);

            return new { };
        }

        [HttpPost("grab/bulk")]
        [Consumes("application/json")]
        public async Task<object> Grab([FromBody] QueueBulkResource resource)
        {
            foreach (var id in resource.Ids)
            {
                var pendingRelease = _pendingReleaseService.FindPendingQueueItem(id);

                if (pendingRelease == null)
                {
                    throw new NotFoundException();
                }

                await _downloadService.DownloadReport(pendingRelease.RemoteAlbum);
            }

            return new { };
        }
    }
}
