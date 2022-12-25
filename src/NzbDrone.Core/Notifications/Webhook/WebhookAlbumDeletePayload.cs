namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookAlbumDeletePayload : WebhookPayload
    {
        public WebhookAlbum Album { get; set; }
        public bool DeletedFiles { get; set; }
    }
}
