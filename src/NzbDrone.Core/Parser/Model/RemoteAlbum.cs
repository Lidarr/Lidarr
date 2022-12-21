using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Parser.Model
{
    public class RemoteAlbum
    {
        public ReleaseInfo Release { get; set; }
        public ParsedAlbumInfo ParsedAlbumInfo { get; set; }
        public Artist Artist { get; set; }
        public List<Album> Albums { get; set; }
        public bool DownloadAllowed { get; set; }
        public TorrentSeedConfiguration SeedConfiguration { get; set; }
        public List<CustomFormat> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public ReleaseSourceType ReleaseSource { get; set; }

        public RemoteAlbum()
        {
            Albums = new List<Album>();
            CustomFormats = new List<CustomFormat>();
        }

        public bool IsRecentAlbum()
        {
            return Albums.Any(e => e.ReleaseDate >= DateTime.UtcNow.Date.AddDays(-14));
        }

        public override string ToString()
        {
            return Release.Title;
        }
    }

    public enum ReleaseSourceType
    {
        Unknown = 0,
        Rss = 1,
        Search = 2,
        UserInvokedSearch = 3,
        InteractiveSearch = 4,
        ReleasePush = 5
    }
}
