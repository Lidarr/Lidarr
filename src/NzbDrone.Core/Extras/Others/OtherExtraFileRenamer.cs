using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Extras.Others
{
    public interface IOtherExtraFileRenamer
    {
        void RenameOtherExtraFile(Artist artist, string path);
    }

    public class OtherExtraFileRenamer : IOtherExtraFileRenamer
    {
        private readonly Logger _logger;
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IArtistService _artistService;
        private readonly IOtherExtraFileService _otherExtraFileService;

        public OtherExtraFileRenamer(IOtherExtraFileService otherExtraFileService,
                                     IArtistService artistService,
                                     IRecycleBinProvider recycleBinProvider,
                                     IDiskProvider diskProvider,
                                     Logger logger)
        {
            _logger = logger;
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _artistService = artistService;
            _otherExtraFileService = otherExtraFileService;
        }

        public void RenameOtherExtraFile(Artist artist, string path)
        {
            if (!_diskProvider.FileExists(path))
            {
                return;
            }

            var relativePath = artist.Path.GetRelativePath(path);

            var otherExtraFile = _otherExtraFileService.FindByPath(relativePath);
            if (otherExtraFile != null)
            {
                var newPath = path + "-orig";

                // Recycle an existing -orig file.
                RemoveOtherExtraFile(artist, newPath);

                // Rename the file to .*-orig
                _diskProvider.MoveFile(path, newPath);
                otherExtraFile.RelativePath = relativePath + "-orig";
                otherExtraFile.Extension += "-orig";
                _otherExtraFileService.Upsert(otherExtraFile);
            }
        }

        private void RemoveOtherExtraFile(Artist artist, string path)
        {
            if (!_diskProvider.FileExists(path))
            {
                return;
            }

            var relativePath = artist.Path.GetRelativePath(path);

            var otherExtraFile = _otherExtraFileService.FindByPath(relativePath);
            if (otherExtraFile != null)
            {
                var subfolder = Path.GetDirectoryName(relativePath);
                _recycleBinProvider.DeleteFile(path, subfolder);
            }
        }
    }
}
