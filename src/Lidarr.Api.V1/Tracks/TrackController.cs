using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lidarr.Http;
using Lidarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Popularity;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Tracks
{
    [V1ApiController]
    public class TrackController : TrackControllerWithSignalR
    {
        private readonly ISongPopularityService _songPopularityService;

        public TrackController(IArtistService artistService,
                             ITrackService trackService,
                             IUpgradableSpecification upgradableSpecification,
                             ICustomFormatCalculationService formatCalculator,
                             ISongPopularityService songPopularityService,
                             IBroadcastSignalRMessage signalRBroadcaster)
            : base(trackService, artistService, upgradableSpecification, formatCalculator, signalRBroadcaster)
        {
            _songPopularityService = songPopularityService;
        }

        [HttpGet("popularity")]
        public Task<SongPopularityResult> GetPopularity([FromQuery] int artistId)
        {
            return _songPopularityService.GetArtistPopularity(_artistService.GetArtist(artistId));
        }

        [HttpGet]
        public List<TrackResource> GetTracks([FromQuery]int? artistId,
            [FromQuery]int? albumId,
            [FromQuery]int? albumReleaseId,
            [FromQuery]List<int> trackIds)
        {
            if (!artistId.HasValue && !trackIds.Any() && !albumId.HasValue && !albumReleaseId.HasValue)
            {
                throw new BadRequestException("One of artistId, albumId, albumReleaseId or trackIds must be provided");
            }

            if (artistId.HasValue && !albumId.HasValue)
            {
                return MapToResource(_trackService.GetTracksByArtist(artistId.Value), false, false);
            }

            if (albumReleaseId.HasValue)
            {
                return MapToResource(_trackService.GetTracksByRelease(albumReleaseId.Value), false, false);
            }

            if (albumId.HasValue)
            {
                return MapToResource(_trackService.GetTracksByAlbum(albumId.Value), false, false);
            }

            return MapToResource(_trackService.GetTracks(trackIds), false, false);
        }

        [HttpPut("monitor")]
        public IActionResult SetTracksMonitored([FromBody] TracksMonitoredResource resource)
        {
            var tracks = _trackService.SetMonitored(resource.TrackIds, resource.Monitored);

            return Accepted(MapToResource(tracks, false, false));
        }

        [HttpGet("capabilities")]
        public IActionResult GetCapabilities()
        {
            return Ok(new { trackSearch = true, selectiveImport = true });
        }
    }
}
