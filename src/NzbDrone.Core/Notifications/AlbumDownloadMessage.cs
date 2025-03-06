using System.Collections.Generic;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Notifications
{
    public class AlbumDownloadMessage
    {
        public string Message { get; set; }
        public Artist Artist { get; set; }
        public Album Album { get; set; }
        public AlbumRelease Release { get; set; }
        public List<TrackFile> TrackFiles { get; set; } = new ();
        public List<TrackFile> OldFiles { get; set; } = new ();
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
