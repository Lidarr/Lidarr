using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamedTrackFile : WebhookTrackFile
    {
        public WebhookRenamedTrackFile(RenamedTrackFile renamedMovie)
            : base(renamedMovie.TrackFile)
        {
            PreviousPath = renamedMovie.PreviousPath;
        }

        public string PreviousPath { get; set; }
    }
}
