using System;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookTrackFile
    {
        public WebhookTrackFile()
        {
        }

        public WebhookTrackFile(TrackFile trackFile)
        {
            Id = trackFile.Id;
            Path = trackFile.Path;
            Quality = trackFile.Quality.Quality.Name;
            QualityVersion = trackFile.Quality.Revision.Version;
            ReleaseGroup = trackFile.ReleaseGroup;
            SceneName = trackFile.SceneName;
            Size = trackFile.Size;
            DateAdded = trackFile.DateAdded;
        }

        public int Id { get; set; }
        public string Path { get; set; }
        public string Quality { get; set; }
        public int QualityVersion { get; set; }
        public string ReleaseGroup { get; set; }
        public string SceneName { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
