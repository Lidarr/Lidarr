using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.TrackImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMoveTrackFiles
    {
        TrackFile MoveTrackFile(TrackFile trackFile, Artist artist);
        TrackFile MoveTrackFile(TrackFile trackFile, LocalTrack localTrack);
        TrackFile CopyTrackFile(TrackFile trackFile, LocalTrack localTrack);
    }

    public class TrackFileMovingService : IMoveTrackFiles
    {
        private readonly ITrackService _trackService;
        private readonly IAlbumService _albumService;
        private readonly IUpdateTrackFileService _updateTrackFileService;
        private readonly IBuildFileNames _buildFileNames;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IImportScript _scriptImportDecider;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public TrackFileMovingService(ITrackService trackService,
                                      IAlbumService albumService,
                                      IUpdateTrackFileService updateTrackFileService,
                                      IBuildFileNames buildFileNames,
                                      IDiskTransferService diskTransferService,
                                      IDiskProvider diskProvider,
                                      IRootFolderWatchingService rootFolderWatchingService,
                                      IMediaFileAttributeService mediaFileAttributeService,
                                      IRootFolderService rootFolderService,
                                      IEventAggregator eventAggregator,
                                      IImportScript scriptImportDecider,
                                      IConfigService configService,
                                      Logger logger)
        {
            _trackService = trackService;
            _albumService = albumService;
            _updateTrackFileService = updateTrackFileService;
            _buildFileNames = buildFileNames;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _rootFolderWatchingService = rootFolderWatchingService;
            _mediaFileAttributeService = mediaFileAttributeService;
            _rootFolderService = rootFolderService;
            _eventAggregator = eventAggregator;
            _scriptImportDecider = scriptImportDecider;
            _configService = configService;
            _logger = logger;
        }

        public TrackFile MoveTrackFile(TrackFile trackFile, Artist artist)
        {
            var tracks = _trackService.GetTracksByFileId(trackFile.Id);
            var album = _albumService.GetAlbum(trackFile.AlbumId);
            var filePath = _buildFileNames.BuildTrackFilePath(tracks, artist, album, trackFile, Path.GetExtension(trackFile.Path));

            EnsureTrackFolder(trackFile, artist, album, filePath);

            _logger.Debug("Renaming track file: {0} to {1}", trackFile, filePath);

            return TransferFile(trackFile, artist, tracks, filePath, TransferMode.Move);
        }

        public TrackFile MoveTrackFile(TrackFile trackFile, LocalTrack localTrack)
        {
            var filePath = _buildFileNames.BuildTrackFilePath(localTrack.Tracks, localTrack.Artist, localTrack.Album, trackFile, Path.GetExtension(trackFile.Path));

            EnsureTrackFolder(trackFile, localTrack, filePath);

            _logger.Debug("Moving track file: {0} to {1}", trackFile.Path, filePath);

            return TransferFile(trackFile, localTrack.Artist, localTrack.Tracks, filePath, TransferMode.Move, localTrack);
        }

        public TrackFile CopyTrackFile(TrackFile trackFile, LocalTrack localTrack)
        {
            var filePath = _buildFileNames.BuildTrackFilePath(localTrack.Tracks, localTrack.Artist, localTrack.Album, trackFile, Path.GetExtension(trackFile.Path));

            EnsureTrackFolder(trackFile, localTrack, filePath);

            if (_configService.CopyUsingHardlinks)
            {
                _logger.Debug("Attempting to hardlink track file: {0} to {1}", trackFile.Path, filePath);
                return TransferFile(trackFile, localTrack.Artist, localTrack.Tracks, filePath, TransferMode.HardLinkOrCopy, localTrack);
            }

            _logger.Debug("Copying track file: {0} to {1}", trackFile.Path, filePath);
            return TransferFile(trackFile, localTrack.Artist, localTrack.Tracks, filePath, TransferMode.Copy, localTrack);
        }

        private TrackFile TransferFile(TrackFile trackFile, Artist artist, List<Track> tracks, string destinationFilePath, TransferMode mode, LocalTrack localTrack = null)
        {
            Ensure.That(trackFile, () => trackFile).IsNotNull();
            Ensure.That(artist, () => artist).IsNotNull();
            Ensure.That(destinationFilePath, () => destinationFilePath).IsValidPath(PathValidationType.CurrentOs);

            var trackFilePath = trackFile.Path;

            if (!_diskProvider.FileExists(trackFilePath))
            {
                throw new FileNotFoundException("Track file path does not exist", trackFilePath);
            }

            if (trackFilePath == destinationFilePath)
            {
                throw new SameFilenameException("File not moved, source and destination are the same", trackFilePath);
            }

            var transfer = true;

            if (localTrack is not null)
            {
                var scriptImportDecision = _scriptImportDecider.TryImport(trackFilePath, destinationFilePath, localTrack, trackFile, mode, null);

                switch (scriptImportDecision)
                {
                    case ScriptImportDecision.DeferMove:
                        break;
                    case ScriptImportDecision.RenameRequested:
                        MoveTrackFile(trackFile, artist);
                        transfer = false;
                        break;
                    case ScriptImportDecision.MoveComplete:
                        transfer = false;
                        break;
                }
            }

            if (transfer)
            {
                _rootFolderWatchingService.ReportFileSystemChangeBeginning(trackFilePath, destinationFilePath);
                _diskTransferService.TransferFile(trackFilePath, destinationFilePath, mode);
            }

            trackFile.Path = destinationFilePath;

            _updateTrackFileService.ChangeFileDateForFile(trackFile, artist, tracks);

            try
            {
                _mediaFileAttributeService.SetFolderLastWriteTime(artist.Path, trackFile.DateAdded);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to set last write time");
            }

            _mediaFileAttributeService.SetFilePermissions(destinationFilePath);

            return trackFile;
        }

        private void EnsureTrackFolder(TrackFile trackFile, LocalTrack localTrack, string filePath)
        {
            EnsureTrackFolder(trackFile, localTrack.Artist, localTrack.Album, filePath);
        }

        private void EnsureTrackFolder(TrackFile trackFile, Artist artist, Album album, string filePath)
        {
            var trackFolder = Path.GetDirectoryName(filePath);
            var artistFolder = artist.Path;
            var rootFolder = _rootFolderService.GetBestRootFolder(artistFolder);

            if (rootFolder == null || rootFolder.Path.IsNullOrWhiteSpace())
            {
                throw new RootFolderNotFoundException($"Root folder was not found, '{artistFolder}' is not a subdirectory of a defined root folder.");
            }

            if (!_diskProvider.FolderExists(rootFolder.Path))
            {
                throw new RootFolderNotFoundException($"Root folder '{rootFolder.Path}' was not found.");
            }

            var changed = false;
            var newEvent = new TrackFolderCreatedEvent(artist, trackFile);

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(artistFolder, trackFolder);

            if (!_diskProvider.FolderExists(artistFolder))
            {
                CreateFolder(artistFolder);
                newEvent.ArtistFolder = artistFolder;
                changed = true;
            }

            if (artistFolder != trackFolder && !_diskProvider.FolderExists(trackFolder))
            {
                CreateFolder(trackFolder);
                newEvent.AlbumFolder = trackFolder;
                newEvent.TrackFolder = trackFolder;
                changed = true;
            }

            if (changed)
            {
                _eventAggregator.PublishEvent(newEvent);
            }
        }

        private void CreateFolder(string directoryName)
        {
            Ensure.That(directoryName, () => directoryName).IsNotNullOrWhiteSpace();

            var parentFolder = new OsPath(directoryName).Directory.FullPath;
            if (!_diskProvider.FolderExists(parentFolder))
            {
                CreateFolder(parentFolder);
            }

            try
            {
                _diskProvider.CreateFolder(directoryName);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Unable to create directory: {0}", directoryName);
            }

            _mediaFileAttributeService.SetFolderPermissions(directoryName);
        }
    }
}
