using System.Linq;
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
        public PagingResource<AlbumResource> GetMissingAlbums(bool includeArtist = false)
        {
            var pagingResource = Request.ReadPagingResourceFromRequest<AlbumResource>();
            var pagingSpec = new PagingSpec<Album>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            var monitoredFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "monitored");

            if (monitoredFilter != null && monitoredFilter.Value == "false")
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Artist.Value.Monitored == false);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Artist.Value.Monitored == true);
            }

            return pagingSpec.ApplyToPage(_albumService.AlbumsWithoutFiles, v => MapToResource(v, includeArtist));
        }
    }
}
