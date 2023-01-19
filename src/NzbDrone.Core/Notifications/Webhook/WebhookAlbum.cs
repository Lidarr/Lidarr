using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookAlbum
    {
        public WebhookAlbum()
        {
        }

        public WebhookAlbum(Album album)
        {
            Id = album.Id;
            MBId = album.ForeignAlbumId;
            Title = album.Title;
            Disambiguation = album.Disambiguation;
            Overview = album.Overview;
            AlbumType = album.AlbumType;
            SecondaryAlbumTypes = album.SecondaryTypes.Select(x => x.Name).ToList();
            Genres = album.Genres;
            ReleaseDate = album.ReleaseDate;
        }

        public int Id { get; set; }
        public string MBId { get; set; }
        public string Title { get; set; }
        public string Disambiguation { get; set; }
        public string Overview { get; set; }
        public string AlbumType { get; set; }
        public List<string> SecondaryAlbumTypes { get; set; }
        public List<string> Genres { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}
