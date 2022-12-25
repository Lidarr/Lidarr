namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookArtistDeletePayload : WebhookPayload
    {
        public WebhookArtist Artist { get; set; }
        public bool DeletedFiles { get; set; }
    }
}
