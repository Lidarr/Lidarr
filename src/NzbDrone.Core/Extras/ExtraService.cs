using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.TrackImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Extras
{
    public interface IExtraService
    {
        void ImportTrack(LocalTrack localTrack, TrackFile trackFile, bool isReadOnly);
        void ImportAlbumExtras(List<ImportDecision<LocalTrack>> importedTracks);
    }

    public class ExtraService : IExtraService,
                                IHandle<MediaCoversUpdatedEvent>,
                                IHandle<TrackFolderCreatedEvent>,
                                IHandle<ArtistScannedEvent>,
                                IHandle<ArtistRenamedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IAlbumService _albumService;
        private readonly ITrackService _trackService;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly List<IManageExtraFiles> _extraFileManagers;
        private readonly AlbumExtraFileManager _albumExtraManager;
        private readonly Logger _logger;

        public ExtraService(IMediaFileService mediaFileService,
                            IAlbumService albumService,
                            ITrackService trackService,
                            IDiskProvider diskProvider,
                            IConfigService configService,
                            IEnumerable<IManageExtraFiles> extraFileManagers,
                            AlbumExtraFileManager albumExtraManager,
                            Logger logger)
        {
            _mediaFileService = mediaFileService;
            _albumService = albumService;
            _trackService = trackService;
            _diskProvider = diskProvider;
            _configService = configService;
            _extraFileManagers = extraFileManagers.OrderBy(e => e.Order).ToList();
            _albumExtraManager = albumExtraManager;
            _logger = logger;
        }

        public void ImportAlbumExtras(List<ImportDecision<LocalTrack>> importedTracks)
        {
            if (!_configService.ImportExtraFiles)
            {
                return;
            }

            var trackDestinationDirs = importedTracks.SelectMany(x => x.Item.Tracks.Select(t => t.TrackFile.Value.Path))
                .GroupBy(f => _diskProvider.GetParentFolder(f));

            var sourceDirs = importedTracks.GroupBy(x => _diskProvider.GetParentFolder(x.Item.Path));
            if (!sourceDirs.Any())
            {
                return;
            }

            string sourceRoot = null;
            string destinationRoot = null;

            try
            {
                sourceRoot = GetCommonParent(sourceDirs.Select(x => x.Key));
                destinationRoot = GetCommonParent(trackDestinationDirs.Select(x => x.Key));
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Common parent dir could not be found, extra files will not be imported", ex);
            }

            var extraFileImports = new Dictionary<string, AlbumExtraFileImport>();
            var trackNames = importedTracks.Select(f => Path.GetFileNameWithoutExtension(f.Item.Path));
            var wantedExtensions = ExtraFileExtensionsList();

            // extra files in track dirs for multi-CD releases
            foreach (var sourceDirImports in sourceDirs)
            {
                var trackFilePath = sourceDirImports.First()
                    .Item?.Tracks?.FirstOrDefault()?.TrackFile?.Value?.Path;
                if (trackFilePath == null)
                {
                    continue;
                }

                var targetDir = sourceDirs.Count() == 1
                    ? destinationRoot
                    : _diskProvider.GetParentFolder(trackFilePath);

                var trackDirFiles = _diskProvider.GetFiles(sourceDirImports.Key, false);
                var trackDirExtraFiles = FilterAlbumExtraFiles(trackDirFiles, trackNames, wantedExtensions);
                foreach (var trackDirExtra in trackDirExtraFiles)
                {
                    var import = AlbumExtraFileImport.AtDestinationDir(trackDirExtra, targetDir);
                    extraFileImports.Add(trackDirExtra, import);
                }

                // nested files under track dirs:
                var subdirFiles = _diskProvider.GetFiles(sourceDirImports.Key, true);
                subdirFiles = FilterAlbumExtraFiles(subdirFiles, trackNames, wantedExtensions);

                foreach (var subdirExtra in subdirFiles.Where(x => !extraFileImports.ContainsKey(x)))
                {
                    var extraFileDirectory = _diskProvider.GetParentFolder(subdirExtra);
                    var relative = sourceDirImports.Key.GetRelativePath(extraFileDirectory);
                    var dest = Path.Combine(targetDir, relative);
                    var import = AlbumExtraFileImport.AtDestinationDir(subdirExtra, dest);
                    extraFileImports.Add(subdirExtra, import);
                }
            }

            if (sourceDirs.Count() > 1)
            {
                // look for common parent dir
                var parentDirs = sourceDirs.GroupBy(x => _diskProvider.GetParentFolder(x.Key));

                if (parentDirs.Count() == 1)
                {
                    var albumDirFiles = _diskProvider.GetFiles(parentDirs.Single().Key, true);
                    var albumExtras = FilterAlbumExtraFiles(albumDirFiles, trackNames, wantedExtensions);

                    foreach (var albumExtraFile in albumExtras.Where(x => !extraFileImports.ContainsKey(x)))
                    {
                        var newImport = AlbumExtraFileImport.AtRelativePathFromSource(albumExtraFile, sourceRoot, destinationRoot);
                        extraFileImports.Add(albumExtraFile, newImport);
                    }
                }
            }

            var firstTrack = importedTracks.First();
            var artist = firstTrack.Item.Artist;
            var albumId = firstTrack.Item.Album.Id;

            _albumExtraManager.ImportAlbumExtras(artist, albumId, extraFileImports.Values);
        }

        public void ImportTrack(LocalTrack localTrack, TrackFile trackFile, bool isReadOnly)
        {
            ImportExtraFiles(localTrack, trackFile, isReadOnly);

            CreateAfterTrackImport(localTrack.Artist, trackFile);
        }

        public void ImportExtraFiles(LocalTrack localTrack, TrackFile trackFile, bool isReadOnly)
        {
            if (!_configService.ImportExtraFiles)
            {
                return;
            }

            var sourcePath = localTrack.Path;
            var sourceFolder = _diskProvider.GetParentFolder(sourcePath);
            var sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
            var files = _diskProvider.GetFiles(sourceFolder, false);
            var wantedExtensions = ExtraFileExtensionsList();

            var matchingFilenames = files.Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(sourceFileName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            var filteredFilenames = new List<string>();
            var hasNfo = false;

            foreach (var matchingFilename in matchingFilenames)
            {
                // Filter out duplicate NFO files
                if (matchingFilename.EndsWith(".nfo", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (hasNfo)
                    {
                        continue;
                    }

                    hasNfo = true;
                }

                filteredFilenames.Add(matchingFilename);
            }

            foreach (var matchingFilename in filteredFilenames)
            {
                var matchingExtension = wantedExtensions.FirstOrDefault(e => matchingFilename.EndsWith(e));

                if (matchingExtension == null)
                {
                    continue;
                }

                try
                {
                    foreach (var extraFileManager in _extraFileManagers)
                    {
                        var extension = Path.GetExtension(matchingFilename);
                        var extraFile = extraFileManager.Import(localTrack.Artist, trackFile, matchingFilename, extension, isReadOnly);

                        if (extraFile != null)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to import extra file: {0}", matchingFilename);
                }
            }
        }

        private void CreateAfterTrackImport(Artist artist, TrackFile trackFile)
        {
            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterTrackImport(artist, trackFile);
            }
        }

        public void Handle(MediaCoversUpdatedEvent message)
        {
            if (message.Updated)
            {
                var artist = message.Artist ?? message.Album.Artist;

                foreach (var extraFileManager in _extraFileManagers)
                {
                    extraFileManager.CreateAfterMediaCoverUpdate(artist);
                }
            }
        }

        public void Handle(ArtistScannedEvent message)
        {
            var artist = message.Artist;

            var trackFiles = GetTrackFiles(artist.Id);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterArtistScan(artist, trackFiles);
            }
        }

        public void Handle(TrackFolderCreatedEvent message)
        {
            var artist = message.Artist;
            var album = _albumService.GetAlbum(message.TrackFile.AlbumId);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterTrackFolder(artist, album, message.ArtistFolder, message.AlbumFolder);
            }
        }

        public void Handle(ArtistRenamedEvent message)
        {
            var artist = message.Artist;
            var trackFiles = GetTrackFiles(artist.Id);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.MoveFilesAfterRename(artist, trackFiles);
            }

            _ = _albumExtraManager.MoveFilesAfterRename(artist, message.RenamedFiles);
        }

        private static IEnumerable<string> FilterAlbumExtraFiles(IEnumerable<string> files,
            IEnumerable<string> trackFileNames,
            IEnumerable<string> wantedExtensions)
        {
            return files
                .Where(x =>
                    wantedExtensions.Any(ext => x.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                    && !trackFileNames.Any(t => t.Equals(Path.GetFileNameWithoutExtension(x), StringComparison.OrdinalIgnoreCase)));
        }

        private List<TrackFile> GetTrackFiles(int artistId)
        {
            var trackFiles = _mediaFileService.GetFilesByArtist(artistId);
            var tracks = _trackService.GetTracksByArtist(artistId);

            foreach (var trackFile in trackFiles)
            {
                var localTrackFile = trackFile;
                trackFile.Tracks = tracks.Where(e => e.TrackFileId == localTrackFile.Id).ToList();
            }

            return trackFiles;
        }

        private List<string> ExtraFileExtensionsList()
        {
            return _configService.ExtraFileExtensions
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim(' ', '.'))
                    .ToList();
        }

        private string GetCommonParent(IEnumerable<string> paths)
        {
            if (paths.Count() == 1)
            {
                return paths.Single();
            }

            var parentDirs = paths.GroupBy(p => _diskProvider.GetParentFolder(p));
            if (parentDirs.Count() == 1)
            {
                return parentDirs.Single().Key;
            }

            // search depth limited to 1+1, parent of parent:
            var parentOfParent = parentDirs.Select(d => _diskProvider.GetParentFolder(d.Key)).GroupBy(i => i);
            if (parentOfParent.Count() == 1)
            {
                return parentOfParent.Single().Key;
            }

            // Look for shortest path and check if this is the parent dir:
            var ordered = parentDirs.OrderBy(x => x.Key.Length);

            var commonParent = ordered.First().Key;
            foreach (var childDir in ordered.Skip(1))
            {
                try
                {
                    _ = commonParent.GetRelativePath(childDir.Key);
                }
                catch (NotParentException ex)
                {
                    throw new ArgumentException(
                        $"Unable to find common parent: child path not under parent candidate '{commonParent}'", nameof(paths), ex);
                }
            }

            return commonParent;
        }
    }
}
