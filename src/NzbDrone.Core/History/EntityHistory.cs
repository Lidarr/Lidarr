using System;
using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public class EntityHistory : ModelBase
    {
        public const string DOWNLOAD_CLIENT = "downloadClient";
        public const string RELEASE_SOURCE = "releaseSource";
        public const string RELEASE_GROUP = "releaseGroup";
        public const string SIZE = "size";
        public const string INDEXER = "indexer";

        public EntityHistory()
        {
            Data = new Dictionary<string, string>();
        }

        public int TrackId { get; set; }
        public int AlbumId { get; set; }
        public int ArtistId { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public DateTime Date { get; set; }
        public Album Album { get; set; }
        public Artist Artist { get; set; }
        public Track Track { get; set; }
        public EntityHistoryEventType EventType { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public string DownloadId { get; set; }
    }

    public enum EntityHistoryEventType
    {
        Unknown = 0,
        Grabbed = 1,
        ArtistFolderImported = 2,
        TrackFileImported = 3,
        DownloadFailed = 4,
        TrackFileDeleted = 5,
        TrackFileRenamed = 6,
        AlbumImportIncomplete = 7,
        DownloadImported = 8,
        TrackFileRetagged = 9,
        DownloadIgnored = 10
    }
}
