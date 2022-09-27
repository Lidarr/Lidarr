namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRetagPayload : WebhookPayload
    {
        public WebhookArtist Artist { get; set; }
        public WebhookTrackFile TrackFile { get; set; }
    }
}
