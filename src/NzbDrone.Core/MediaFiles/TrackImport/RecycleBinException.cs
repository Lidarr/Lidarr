using System;
using System.IO;

namespace NzbDrone.Core.MediaFiles.TrackImport
{
    public class RecycleBinException : DirectoryNotFoundException
    {
        public RecycleBinException()
        {
        }

        public RecycleBinException(string message)
            : base(message)
        {
        }

        public RecycleBinException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
