using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookGrabPayload : WebhookPayload
    {
        public WebhookArtist Artist { get; set; }
        public List<WebhookAlbum> Albums { get; set; }
        public WebhookRelease Release { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadId { get; set; }
    }
}
