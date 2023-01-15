using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class ArtistRenamedEvent : IEvent
    {
        public Artist Artist { get; private set; }
        public List<RenamedTrackFile> RenamedFiles { get; private set; }

        public ArtistRenamedEvent(Artist artist, List<RenamedTrackFile> renamedFiles)
        {
            Artist = artist;
            RenamedFiles = renamedFiles;
        }
    }
}
