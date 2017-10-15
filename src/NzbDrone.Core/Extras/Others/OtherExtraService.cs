using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Extras.Others
{
    public class OtherExtraService : ExtraFileManager<OtherExtraFile>
    {
        private readonly IOtherExtraFileService _otherExtraFileService;

        public OtherExtraService(IConfigService configService,
                                 IDiskProvider diskProvider,
                                 IDiskTransferService diskTransferService,
                                 IOtherExtraFileService otherExtraFileService,
                                 Logger logger)
            : base(configService, diskProvider, diskTransferService, logger)
        {
            _otherExtraFileService = otherExtraFileService;
        }

        public override int Order => 2;

        public override IEnumerable<ExtraFile> CreateAfterArtistScan(Artist series, List<TrackFile> episodeFiles)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> CreateAfterEpisodeImport(Artist series, TrackFile episodeFile)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> CreateAfterEpisodeImport(Artist series, string seriesFolder, string seasonFolder)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> MoveFilesAfterRename(Artist series, List<TrackFile> episodeFiles)
        {
            var extraFiles = _otherExtraFileService.GetFilesByArtist(series.Id);
            var movedFiles = new List<OtherExtraFile>();

            foreach (var episodeFile in episodeFiles)
            {
                var extraFilesForEpisodeFile = extraFiles.Where(m => m.TrackFileId == episodeFile.Id).ToList();

                foreach (var extraFile in extraFilesForEpisodeFile)
                {
                    movedFiles.AddIfNotNull(MoveFile(series, episodeFile, extraFile));
                }
            }

            _otherExtraFileService.Upsert(movedFiles);

            return movedFiles;
        }

        public override ExtraFile Import(Artist series, TrackFile episodeFile, string path, string extension, bool readOnly)
        {
            // If the extension is .nfo we need to change it to .nfo-orig
            if (Path.GetExtension(path).Equals(".nfo"))
            {
                extension += "-orig";
            }

            var extraFile = ImportFile(series, episodeFile, path, readOnly, extension, null);

            _otherExtraFileService.Upsert(extraFile);

            return extraFile;
        }
    }
}
