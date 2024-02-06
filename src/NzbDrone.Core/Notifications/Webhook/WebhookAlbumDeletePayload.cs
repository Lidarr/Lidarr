namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookAlbumDeletePayload : WebhookPayload
    {
        public WebhookArtist Artist { get; set; }
        public WebhookAlbum Album { get; set; }
        public bool DeletedFiles { get; set; }
    }
}
