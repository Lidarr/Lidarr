using NzbDrone.Core.Music;

namespace NzbDrone.Core.Notifications
{
    public class ArtistAddMessage
    {
        public string Message { get; set; }
        public Artist Artist { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
