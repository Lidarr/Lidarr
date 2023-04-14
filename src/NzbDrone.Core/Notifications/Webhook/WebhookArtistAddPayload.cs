namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookArtistAddPayload : WebhookPayload
    {
        public WebhookArtist Artist { get; set; }
    }
}
