using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Music.Events
{
    public class ArtistAddCompletedEvent : IEvent
    {
        public Artist Artist { get; private set; }

        public ArtistAddCompletedEvent(Artist artist)
        {
            Artist = artist;
        }
    }
}
