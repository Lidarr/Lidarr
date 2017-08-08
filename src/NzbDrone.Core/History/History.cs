﻿using System;
using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.History
{
    public class History : ModelBase
    {
        public const string DOWNLOAD_CLIENT = "downloadClient";

        public History()
        {
            Data = new Dictionary<string, string>();
        }
        
        public int AlbumId { get; set; }
        public int ArtistId { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public DateTime Date { get; set; }
        public Album Album { get; set; }
        public Artist Artist { get; set; }
        public HistoryEventType EventType { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public string DownloadId { get; set; }

    }

    public enum HistoryEventType
    {
        Unknown = 0,
        Grabbed = 1,
        SeriesFolderImported = 2,
        DownloadFolderImported = 3,
        DownloadFailed = 4,
        EpisodeFileDeleted = 5
    }
}