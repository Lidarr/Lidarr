﻿using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.MediaFiles.TrackImport;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Manual
{
    public class ManualImportCommand : Command
    {
        public List<ManualImportFile> Files { get; set; }

        public override bool SendUpdatesToClient => true;

        public ImportMode ImportMode { get; set; }
    }
}
