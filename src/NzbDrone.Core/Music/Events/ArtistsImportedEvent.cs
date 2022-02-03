using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Music.Events;

public class ArtistsImportedEvent : IEvent
{
    public List<Artist> Artists { get; private set; }
    public bool DoRefresh { get; private set; }

    public ArtistsImportedEvent(List<Artist> artists, bool doRefresh = true)
    {
        Artists = artists;
        DoRefresh = doRefresh;
    }
}
