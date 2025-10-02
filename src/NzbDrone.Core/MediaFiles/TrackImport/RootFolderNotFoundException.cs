using System;
using System.IO;

namespace NzbDrone.Core.MediaFiles.TrackImport
{
    public class RootFolderNotFoundException : DirectoryNotFoundException
    {
        public RootFolderNotFoundException()
        {
        }

        public RootFolderNotFoundException(string message)
            : base(message)
        {
        }

        public RootFolderNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
