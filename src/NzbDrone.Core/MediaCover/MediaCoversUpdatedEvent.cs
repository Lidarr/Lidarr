using NzbDrone.Common.Messaging;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.MediaCover;

public class MediaCoversUpdatedEvent : IEvent
{
    public Artist Artist { get; set; }
    public Album Album { get; set; }
    public bool Updated { get; set; }

    public MediaCoversUpdatedEvent(Artist artist, bool updated)
    {
        Artist = artist;
        Updated = updated;
    }

    public MediaCoversUpdatedEvent(Album album, bool updated)
    {
        Album = album;
        Updated = updated;
    }
}
