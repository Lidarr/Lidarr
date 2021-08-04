using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Music;

namespace Lidarr.Api.V1.AlbumStudio
{
    [V1ApiController]
    public class AlbumStudioController : Controller
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumMonitoredService _albumMonitoredService;

        public AlbumStudioController(IArtistService artistService, IAlbumMonitoredService albumMonitoredService)
        {
            _artistService = artistService;
            _albumMonitoredService = albumMonitoredService;
        }

        [HttpPost]
        public IActionResult UpdateAll([FromBody] AlbumStudioResource request)
        {
            var artistToUpdate = _artistService.GetArtists(request.Artist.Select(s => s.Id));

            foreach (var s in request.Artist)
            {
                var artist = artistToUpdate.Single(c => c.Id == s.Id);

                if (s.Monitored.HasValue)
                {
                    artist.Monitored = s.Monitored.Value;
                }

                if (request.MonitoringOptions != null && request.MonitoringOptions.Monitor == MonitorTypes.None)
                {
                    artist.Monitored = false;
                }

                _albumMonitoredService.SetAlbumMonitoredStatus(artist, request.MonitoringOptions);
            }

            return Accepted();
        }
    }
}
