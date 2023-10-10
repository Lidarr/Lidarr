using Lidarr.Api.V1.Albums;
using Lidarr.Http;
using Lidarr.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.ArtistStats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Wanted
{
    [V1ApiController("wanted/missing")]
    public class MissingController : AlbumControllerWithSignalR
    {
        public MissingController(IAlbumService albumService,
                             IArtistStatisticsService artistStatisticsService,
                             IMapCoversToLocal coverMapper,
                             IUpgradableSpecification upgradableSpecification,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(albumService, artistStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
        }

        [HttpGet]
        [Produces("application/json")]
        public PagingResource<AlbumResource> GetMissingAlbums([FromQuery] PagingRequestResource paging, bool includeArtist = false, bool monitored = true)
        {
            var pagingResource = new PagingResource<AlbumResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<AlbumResource, Album>("releaseDate", SortDirection.Descending);

            if (monitored)
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Artist.Value.Monitored == true);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Artist.Value.Monitored == false);
            }

            return pagingSpec.ApplyToPage(_albumService.AlbumsWithoutFiles, v => MapToResource(v, includeArtist));
        }
    }
}
