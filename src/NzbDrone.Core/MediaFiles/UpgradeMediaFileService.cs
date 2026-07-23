using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.TrackImport;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpgradeMediaFiles
    {
        TrackFileMoveResult UpgradeTrackFile(TrackFile trackFile, LocalTrack localTrack, bool copyOnly = false);
    }

    public class UpgradeMediaFileService : IUpgradeMediaFiles
    {
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IAudioTagService _audioTagService;
        private readonly IMoveTrackFiles _trackFileMover;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public UpgradeMediaFileService(IRecycleBinProvider recycleBinProvider,
                                       IMediaFileService mediaFileService,
                                       IAudioTagService audioTagService,
                                       IMoveTrackFiles trackFileMover,
                                       IDiskProvider diskProvider,
                                       Logger logger)
        {
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _audioTagService = audioTagService;
            _trackFileMover = trackFileMover;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public TrackFileMoveResult UpgradeTrackFile(TrackFile trackFile, LocalTrack localTrack, bool copyOnly = false)
        {
            var moveFileResult = new TrackFileMoveResult();
            var existingFiles = localTrack.Tracks
                                            .Where(e => e.TrackFileId > 0)
                                            .Select(e => e.TrackFile.Value)
                                            .Where(e => e != null)
                                            .GroupBy(e => e.Id)
                                            .ToList();

            var rootFolder = _diskProvider.GetParentFolder(localTrack.Artist.Path);

            // If there are existing track files and the root folder is missing, throw, so the old file isn't left behind during the import process.
            if (existingFiles.Any() && !_diskProvider.FolderExists(rootFolder))
            {
                throw new RootFolderNotFoundException($"Root folder '{rootFolder}' was not found.");
            }

            // Transfer the new file first so a failure (disk full, permissions, network share drop) can't
            // leave the old file deleted with no replacement written. The most common case - an upgrade
            // that computes the same destination path as the file it's replacing - collides and throws
            // DestinationAlreadyExistsException; only then do we clear the old file(s) out of the way and
            // retry, matching the previous delete-first behaviour for that specific case.
            try
            {
                MoveOrCopy(trackFile, localTrack, copyOnly, moveFileResult);
            }
            catch (DestinationAlreadyExistsException) when (existingFiles.Any())
            {
                RemoveExistingFiles(existingFiles, rootFolder, moveFileResult);
                MoveOrCopy(trackFile, localTrack, copyOnly, moveFileResult);
            }

            RemoveExistingFiles(existingFiles.Where(g => moveFileResult.OldFiles.All(f => f.Id != g.Key)), rootFolder, moveFileResult);

            _audioTagService.WriteTags(trackFile, true);

            return moveFileResult;
        }

        private void MoveOrCopy(TrackFile trackFile, LocalTrack localTrack, bool copyOnly, TrackFileMoveResult moveFileResult)
        {
            moveFileResult.TrackFile = copyOnly
                ? _trackFileMover.CopyTrackFile(trackFile, localTrack)
                : _trackFileMover.MoveTrackFile(trackFile, localTrack);
        }

        private void RemoveExistingFiles(IEnumerable<IGrouping<int, TrackFile>> existingFiles, string rootFolder, TrackFileMoveResult moveFileResult)
        {
            foreach (var existingFile in existingFiles)
            {
                var file = existingFile.First();
                var trackFilePath = file.Path;
                var subfolder = rootFolder.GetRelativePath(_diskProvider.GetParentFolder(trackFilePath));

                if (_diskProvider.FileExists(trackFilePath))
                {
                    _logger.Debug("Removing existing track file: {0}", file);
                    _recycleBinProvider.DeleteFile(trackFilePath, subfolder);
                }
                else
                {
                    _logger.Warn("Existing track file missing from disk: {0}", trackFilePath);
                }

                moveFileResult.OldFiles.Add(file);
                _mediaFileService.Delete(file, DeleteMediaFileReason.Upgrade);
            }
        }
    }
}
