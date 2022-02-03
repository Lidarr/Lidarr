using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Music.Events;

public class ArtistsDeletedEvent : IEvent
{
    public List<Artist> Artists { get; private set; }
    public bool DeleteFiles { get; private set; }
    public bool AddImportListExclusion { get; private set; }

    public ArtistsDeletedEvent(List<Artist> artists, bool deleteFiles, bool addImportListExclusion)
    {
        Artists = artists;
        DeleteFiles = deleteFiles;
        AddImportListExclusion = addImportListExclusion;
    }
}
