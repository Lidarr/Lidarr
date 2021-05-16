using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Tags;

namespace Lidarr.Api.V1.Calendar
{
    [V1FeedController("calendar")]
    public class CalendarFeedController : Controller
    {
        private readonly IAlbumService _albumService;
        private readonly IArtistService _artistService;
        private readonly ITagService _tagService;

        public CalendarFeedController(IAlbumService albumService, IArtistService artistService, ITagService tagService)
        {
            _albumService = albumService;
            _artistService = artistService;
            _tagService = tagService;
        }

        [HttpGet("Lidarr.ics")]
        public IActionResult GetCalendarFeed(int pastDays = 7, int futureDays = 28, string tagList = "", bool unmonitored = false)
        {
            var start = DateTime.Today.AddDays(-pastDays);
            var end = DateTime.Today.AddDays(futureDays);
            var tags = new List<int>();

            if (tagList.IsNotNullOrWhiteSpace())
            {
                tags.AddRange(tagList.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            var albums = _albumService.AlbumsBetweenDates(start, end, unmonitored);
            var calendar = new Ical.Net.Calendar
            {
                ProductId = "-//lidarr.audio//Lidarr//EN"
            };

            var calendarName = "Lidarr Music Schedule";
            calendar.AddProperty(new CalendarProperty("NAME", calendarName));
            calendar.AddProperty(new CalendarProperty("X-WR-CALNAME", calendarName));

            foreach (var album in albums.OrderBy(v => v.ReleaseDate.Value))
            {
                var artist = _artistService.GetArtist(album.ArtistId); // Temp fix TODO: Figure out why Album.Artist is not populated during AlbumsBetweenDates Query

                if (tags.Any() && tags.None(artist.Tags.Contains))
                {
                    continue;
                }

                var occurrence = calendar.Create<CalendarEvent>();
                occurrence.Uid = "Lidarr_album_" + album.Id;

                //occurrence.Status = album.HasFile ? EventStatus.Confirmed : EventStatus.Tentative;
                occurrence.Description = album.Overview;
                occurrence.Categories = album.Genres;

                occurrence.Start = new CalDateTime(album.ReleaseDate.Value.ToLocalTime()) { HasTime = false };

                occurrence.Summary = $"{artist.Name} - {album.Title}";
            }

            var serializer = (IStringSerializer)new SerializerFactory().Build(calendar.GetType(), new SerializationContext());
            var icalendar = serializer.SerializeToString(calendar);

            return Content(icalendar, "text/calendar");
        }
    }
}
