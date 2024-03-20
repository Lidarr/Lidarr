using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Extras.Files
{
    public class AlbumExtraFileManager
    {
        private readonly IConfigService _configService;
        private readonly INamingConfigService _namingConfigService;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IOtherExtraFileService _otherExtraFileService;
        private readonly Logger _logger;

        private static readonly Regex _albumDirRegex = new Regex(@"{Album.+?Title}.*?\/.*?track", RegexOptions.IgnoreCase);

        public AlbumExtraFileManager(
            IConfigService configService,
            INamingConfigService namingConfigService,
            IDiskTransferService diskTransferService,
            IDiskProvider diskProvider,
            IOtherExtraFileService otherExtraFileService,
            Logger logger)
        {
            _configService = configService;
            _namingConfigService = namingConfigService;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _otherExtraFileService = otherExtraFileService;
            _logger = logger;
        }

        public IEnumerable<ExtraFile> ImportAlbumExtras(Artist artist, int albumId, IEnumerable<AlbumExtraFileImport> extraFileImports)
        {
            var namingConfig = _namingConfigService.GetConfig();
            if (!namingConfig.RenameTracks)
            {
                _logger.Debug($"File renaming is deactivated, skipping {extraFileImports.Count()} album extras");
                return new List<ExtraFile>();
            }

            var albumDirInStandardFormat = _albumDirRegex.IsMatch(namingConfig.StandardTrackFormat);
            if (!albumDirInStandardFormat)
            {
                _logger.Debug($"Track template does not include an album dir, skipping {extraFileImports.Count()} album extras");
                return new List<ExtraFile>();
            }

            var albumDirInMultiDiscFormat = _albumDirRegex.IsMatch(namingConfig.MultiDiscTrackFormat);
            if (!albumDirInMultiDiscFormat)
            {
                _logger.Debug($"Multi-disc template does not include an album dir, skipping {extraFileImports.Count()} album extras");
                return new List<ExtraFile>();
            }

            try
            {
                var result = new List<OtherExtraFile>(extraFileImports.Count());
                foreach (var extraFileImport in extraFileImports)
                {
                    var file = ImportSingleFile(artist, albumId, extraFileImport.SourcePath, extraFileImport.DestinationPath);
                    result.Add(file);
                }

                _otherExtraFileService.Upsert(result.ToList());

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to import {extraFileImports.Count()} album extra files for artist '{artist.CleanName}'");
                return new List<ExtraFile>();
            }
        }

        public IEnumerable<ExtraFile> MoveFilesAfterRename(Artist artist, List<RenamedTrackFile> trackFiles)
        {
            var extraFiles = _otherExtraFileService.GetFilesByArtist(artist.Id);
            if (!extraFiles.Any())
            {
                return new List<ExtraFile>();
            }

            _logger.Debug($"Found {extraFiles.Count} extra files for artist '{artist.Name}'");

            var movedFiles = new List<OtherExtraFile>();

            try
            {
                foreach (var albumTracks in trackFiles.GroupBy(x => x.TrackFile.AlbumId))
                {
                    var albumFiles = MoveAlbumExtraFiles(artist, extraFiles.Where(x => x.AlbumId == albumTracks.Key), albumTracks);
                    _otherExtraFileService.Upsert(albumFiles);

                    movedFiles.AddRange(albumFiles);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Moving album extras for artist '{artist.Name}' failed");
                return new List<ExtraFile>();
            }

            _logger.Info($"Moved {movedFiles.Count} extra files on rename for '{artist.Name}'");

            return movedFiles;
        }

        private OtherExtraFile ImportSingleFile(Artist artist, int albumId, string sourcePath, string destinationPath)
        {
            var transferMode = _configService.CopyUsingHardlinks ? TransferMode.HardLinkOrCopy : TransferMode.Copy;

            if (!sourcePath.PathEquals(destinationPath))
            {
                _diskProvider.CreateFolder(_diskProvider.GetParentFolder(destinationPath));
                _diskTransferService.TransferFile(sourcePath, destinationPath, transferMode, true);
            }

            var extension = Path.GetExtension(destinationPath);

            return new OtherExtraFile
            {
                ArtistId = artist.Id,
                AlbumId = albumId,
                TrackFileId = null,
                RelativePath = artist.Path.GetRelativePath(destinationPath),
                Extension = extension,
            };
        }

        private List<OtherExtraFile> MoveAlbumExtraFiles(Artist artist, IEnumerable<OtherExtraFile> extraFiles, IGrouping<int, RenamedTrackFile> albumTracks)
        {
            var movedFiles = new List<OtherExtraFile>();
            var previousTrackDirs = albumTracks.GroupBy(x => _diskProvider.GetParentFolder(x.PreviousPath));

            // extra files in track directories should stay together with the tracks:
            foreach (var dir in previousTrackDirs)
            {
                var relativeTrackDir = artist.Path.GetRelativePath(dir.Key);
                var extrasUnderTrackDir = extraFiles.Where(
                    x => x.RelativePath.StartsWithIgnoreCase(relativeTrackDir));

                var oldRelative = artist.Path.GetRelativePath(_diskProvider.GetParentFolder(dir.First().PreviousPath));
                var newRelative = artist.Path.GetRelativePath(_diskProvider.GetParentFolder(dir.First().TrackFile.Path));
                foreach (var extraFile in extrasUnderTrackDir)
                {
                    var oldFilePath = Path.Combine(artist.Path, extraFile.RelativePath);

                    var updatedRelativePath = extraFile.RelativePath.Replace(oldRelative, newRelative);
                    extraFile.RelativePath = updatedRelativePath;

                    var newFilePath = Path.Combine(artist.Path, updatedRelativePath);
                    MoveToNewDir(oldFilePath, newFilePath);

                    movedFiles.Add(extraFile);
                }
            }

            // move remaining files to new album dir:
            var remainingExtraFiles = extraFiles.Where(x => !movedFiles.Any(f => f.Id == x.Id));
            var newTrackDirs = albumTracks.GroupBy(x => _diskProvider.GetParentFolder(x.TrackFile.Path));

            if (remainingExtraFiles.Any()
                && previousTrackDirs.Count() > 1
                && newTrackDirs.Count() > 1)
            {
                var oldParentDir = previousTrackDirs.First().Key.GetParentPath();
                var newParentDir = newTrackDirs.First().Key.GetParentPath();

                if (previousTrackDirs.All(d => d.Key.GetParentPath() == oldParentDir)
                    && newTrackDirs.All(d => d.Key.GetParentPath() == newParentDir))
                {
                    var oldRelative = artist.Path.GetRelativePath(oldParentDir);
                    var newRelative = artist.Path.GetRelativePath(newParentDir);

                    foreach (var extraFile in remainingExtraFiles)
                    {
                        var oldPath = Path.Combine(artist.Path, extraFile.RelativePath);

                        var newExtraRelativePath = extraFile.RelativePath.Replace(oldRelative, newRelative);
                        var newFilePath = Path.Combine(artist.Path, newExtraRelativePath);

                        MoveToNewDir(oldPath, newFilePath);

                        extraFile.RelativePath = newExtraRelativePath;
                        movedFiles.Add(extraFile);
                    }
                }
            }

            return movedFiles;
        }

        private void MoveToNewDir(string oldFilePath, string newFilePath)
        {
            _diskProvider.CreateFolder(_diskProvider.GetParentFolder(newFilePath));
            _diskProvider.MoveFile(oldFilePath, newFilePath);
        }
    }
}
