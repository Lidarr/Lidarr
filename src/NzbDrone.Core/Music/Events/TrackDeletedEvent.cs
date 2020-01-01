using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Music.Events
{
    public class TrackDeletedEvent : IEvent
    {
        public Track Track { get; private set; }

        public TrackDeletedEvent(Track track)
        {
            Track = track;
        }
    }
}
