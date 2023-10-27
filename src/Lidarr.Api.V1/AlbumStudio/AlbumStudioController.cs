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
        [Consumes("application/json")]
        public IActionResult UpdateAll([FromBody] AlbumStudioResource resource)
        {
            var artistToUpdate = _artistService.GetArtists(resource.Artist.Select(s => s.Id));

            foreach (var s in resource.Artist)
            {
                var artist = artistToUpdate.Single(c => c.Id == s.Id);

                if (s.Monitored.HasValue)
                {
                    artist.Monitored = s.Monitored.Value;
                }

                if (resource.MonitoringOptions is { Monitor: MonitorTypes.None })
                {
                    artist.Monitored = false;
                }

                if (resource.MonitorNewItems.HasValue)
                {
                    artist.MonitorNewItems = resource.MonitorNewItems.Value;
                }

                _albumMonitoredService.SetAlbumMonitoredStatus(artist, resource.MonitoringOptions);
            }

            return Accepted(new object());
        }
    }
}
