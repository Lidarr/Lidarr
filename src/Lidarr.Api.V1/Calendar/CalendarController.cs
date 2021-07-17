using System;
using System.Collections.Generic;
using System.Linq;
using Lidarr.Api.V1.Albums;
using Lidarr.Http;
using Lidarr.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.ArtistStats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Calendar
{
    [V1ApiController]
    public class CalendarController : AlbumControllerWithSignalR
    {
        public CalendarController(IAlbumService albumService,
                              IArtistStatisticsService artistStatisticsService,
                              IMapCoversToLocal coverMapper,
                              IUpgradableSpecification upgradableSpecification,
                              IBroadcastSignalRMessage signalRBroadcaster)
        : base(albumService, artistStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
        }

        [HttpGet]
        public List<AlbumResource> GetCalendar(DateTime? start, DateTime? end, bool unmonitored = false, bool includeArtist = false)
        {
            //TODO: Add Album Image support to AlbumControllerWithSignalR
            var includeAlbumImages = Request.GetBooleanQueryParameter("includeAlbumImages");

            var startUse = start ?? DateTime.Today;
            var endUse = end ?? DateTime.Today.AddDays(2);

            var resources = MapToResource(_albumService.AlbumsBetweenDates(startUse, endUse, unmonitored), includeArtist);

            return resources.OrderBy(e => e.ReleaseDate).ToList();
        }
    }
}
