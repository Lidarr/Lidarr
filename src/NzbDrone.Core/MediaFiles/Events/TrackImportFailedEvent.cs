using System;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class TrackImportFailedEvent : IEvent
    {
        public Exception Exception { get; set; }
        public LocalTrack TrackInfo { get; }
        public bool NewDownload { get; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; }

        public TrackImportFailedEvent(Exception exception, LocalTrack trackInfo, bool newDownload, DownloadClientItem downloadClientItem)
        {
            Exception = exception;
            TrackInfo = trackInfo;
            NewDownload = newDownload;

            if (downloadClientItem != null)
            {
                DownloadClientInfo = downloadClientItem.DownloadClientInfo;
                DownloadId = downloadClientItem.DownloadId;
            }
        }
    }
}
