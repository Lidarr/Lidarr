using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Lidarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Tracks
{
    [V1ApiController]
    public class TrackController : TrackControllerWithSignalR
    {
        public TrackController(IArtistService artistService,
                             ITrackService trackService,
                             IUpgradableSpecification upgradableSpecification,
                             IBroadcastSignalRMessage signalRBroadcaster)
            : base(trackService, artistService, upgradableSpecification, signalRBroadcaster)
        {
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
    }
}
