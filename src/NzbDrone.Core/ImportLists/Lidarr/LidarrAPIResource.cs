using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists.Lidarr
{
    public class LidarrArtist
    {
        public int Id { get; set; }
        public string ArtistName { get; set; }
        public string ForeignArtistId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public int QualityProfileId { get; set; }
        public HashSet<int> Tags { get; set; }
    }

    public class LidarrAlbum
    {
        public int ArtistId { get; set; }
        public string Title { get; set; }
        public string ForeignAlbumId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public LidarrArtist Artist { get; set; }
    }

    public class LidarrProfile
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class LidarrTag
    {
        public string Label { get; set; }
        public int Id { get; set; }
    }
}
