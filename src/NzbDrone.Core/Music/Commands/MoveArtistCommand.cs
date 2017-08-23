﻿using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Music.Commands
{
    public class MoveArtistCommand : Command
    {
        public int ArtistId { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string DestinationRootFolder { get; set; }
    }
}
