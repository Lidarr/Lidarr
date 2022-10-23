namespace NzbDrone.Core.Notifications.Xbmc.Model
{
    public class SourceResponse
    {
        public string Id { get; set; }
        public string JsonRpc { get; set; }
        public SourceResult Result { get; set; }
    }
}
