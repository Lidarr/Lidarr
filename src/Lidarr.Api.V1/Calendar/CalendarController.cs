using System;
using System.Collections.Generic;
using System.Linq;
using Lidarr.Api.V1.Albums;
using Lidarr.Http;
using Lidarr.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ArtistStats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;
using NzbDrone.Core.Tags;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Calendar
{
    [V1ApiController]
    public class CalendarController : AlbumControllerWithSignalR
    {
        private readonly IArtistService _artistService;
        private readonly ITagService _tagService;

        public CalendarController(IAlbumService albumService,
                              IArtistService artistService,
                              IArtistStatisticsService artistStatisticsService,
                              IMapCoversToLocal coverMapper,
                              IUpgradableSpecification upgradableSpecification,
                              ITagService tagService,
                              IBroadcastSignalRMessage signalRBroadcaster)
        : base(albumService, artistStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
            _artistService = artistService;
            _tagService = tagService;
        }

        [HttpGet]
        [Produces("application/json")]
        public List<AlbumResource> GetCalendar(DateTime? start, DateTime? end, bool unmonitored = false, bool includeArtist = false, string tags = "")
        {
            // TODO: Add Album Image support to AlbumControllerWithSignalR
            var includeAlbumImages = Request.GetBooleanQueryParameter("includeAlbumImages");

            var startUse = start ?? DateTime.Today;
            var endUse = end ?? DateTime.Today.AddDays(2);
            var albums = _albumService.AlbumsBetweenDates(startUse, endUse, unmonitored);
            var allArtists = _artistService.GetAllArtists();
            var parsedTags = new List<int>();
            var result = new List<Album>();

            if (tags.IsNotNullOrWhiteSpace())
            {
                parsedTags.AddRange(tags.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            foreach (var album in albums)
            {
                var artist = allArtists.SingleOrDefault(s => s.Id == album.ArtistId);

                if (artist == null)
                {
                    continue;
                }

                if (parsedTags.Any() && parsedTags.None(artist.Tags.Contains))
                {
                    continue;
                }

                result.Add(album);
            }

            var resources = MapToResource(result, includeArtist);

            return resources.OrderBy(e => e.ReleaseDate).ToList();
        }
    }
}
