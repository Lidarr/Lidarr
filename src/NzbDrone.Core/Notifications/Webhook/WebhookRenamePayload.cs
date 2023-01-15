using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamePayload : WebhookPayload
    {
        public WebhookArtist Artist { get; set; }
        public List<WebhookRenamedTrackFile> RenamedTrackFiles { get; set; }
    }
}
