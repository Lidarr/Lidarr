using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.TrackImport.Manual
{
    public class ManualImportCommand : Command
    {
        public List<ManualImportFile> Files { get; set; }

        public override bool SendUpdatesToClient => true;

        public ImportMode ImportMode { get; set; }
    }
}
