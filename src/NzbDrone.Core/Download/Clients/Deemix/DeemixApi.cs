using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Download.Clients.Deemix
{
    public class DeemixQueueItem
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Cover { get; set; }
        public bool Explicit { get; set; }
        public int Size { get; set; }
        public string ExtrasPath { get; set; }
        public int Downloaded { get; set; }
        public bool Failed { get; set; }
        public List<object> Errors { get; set; }
        public int Progress { get; set; }
        public List<string> Files { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public string Bitrate { get; set; }
        public string Uuid { get; set; }
        public int? Ack { get; set; }
    }

    public class DeemixQueueUpdate
    {
        public string Uuid { get; set; }
        public string DownloadPath { get; set; }
        public string ExtrasPath { get; set; }
        public int? Progress { get; set; }
    }

    public class DeemixQueue
    {
        public List<string> Queue { get; set; }
        public List<string> QueueComplete { get; set; }
        public Dictionary<string, DeemixQueueItem> QueueList { get; set; }
        public string CurrentItem { get; set; }
    }

    public class DeemixSearchResponse
    {
        public IList<DeemixGwAlbum> Data { get; set; }
        public int Total { get; set; }
        public string Next { get; set; }
        public int? Ack { get; set; }
    }

    public class ExplicitAlbumContent
    {
        [JsonProperty("EXPLICIT_LYRICS_STATUS")]
        public int ExplicitLyrics { get; set; }

        [JsonProperty("EXPLICIT_COVER_STATUS")]
        public int ExplicitCover { get; set; }
    }

    public class DeemixGwAlbum
    {
        [JsonProperty("ALB_ID")]
        public string AlbumId { get; set; }
        [JsonProperty("ALB_TITLE")]
        public string AlbumTitle { get; set; }
        [JsonProperty("ALB_PICTURE")]
        public string AlbumPicture { get; set; }
        public bool Available { get; set; }
        [JsonProperty("ART_ID")]
        public string ArtistId { get; set; }
        [JsonProperty("ART_NAME")]
        public string ArtistName { get; set; }
        [JsonProperty("EXPLICIT_ALBUM_CONTENT")]
        public ExplicitAlbumContent ExplicitAlbumContent { get; set; }

        // These two are string not DateTime since sometimes Deemix provides invalid values (like 0000-00-00)
        [JsonProperty("PHYSICAL_RELEASE_DATE")]
        public string PhysicalReleaseDate { get; set; }
        [JsonProperty("DIGITAL_RELEASE_DATE")]
        public string DigitalReleaseDate { get; set; }

        public string Type { get; set; }
        [JsonProperty("ARTIST_IS_DUMMY")]
        public bool ArtistIsDummy { get; set; }
        [JsonProperty("NUMBER_TRACK")]
        public string TrackCount { get; set; }
        [JsonProperty("DURATION")]
        public int DurationInSeconds { get; set; }

        public string Version { get; set; }
        public string Link { get; set; }

        public bool Explicit => ExplicitAlbumContent?.ExplicitLyrics != 0 || ExplicitAlbumContent.ExplicitCover != 0;
    }
}
