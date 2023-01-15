using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Music.Events
{
    public class AlbumDeletedEvent : IEvent
    {
        public Album Album { get; private set; }
        public bool DeleteFiles { get; private set; }
        public bool AddImportListExclusion { get; private set; }
        public List<TrackFile> TrackFilesToDelete { get; set; }

        public AlbumDeletedEvent(Album album, bool deleteFiles, bool addImportListExclusion)
        {
            Album = album;
            DeleteFiles = deleteFiles;
            AddImportListExclusion = addImportListExclusion;
        }
    }
}
