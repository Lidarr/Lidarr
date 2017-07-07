﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRenameTrackFileService
    {
        List<RenameTrackFilePreview> GetRenamePreviews(int artistId);
        List<RenameTrackFilePreview> GetRenamePreviews(int artistId, int albumId);
    }

    public class RenameTrackFileService : IRenameTrackFileService,
                                            IExecute<RenameFilesCommand>,
                                            IExecute<RenameArtistCommand>
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMoveTrackFiles _trackFileMover;
        private readonly IEventAggregator _eventAggregator;
        private readonly ITrackService _trackService;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public RenameTrackFileService(IArtistService artistService,
                                        IAlbumService albumService,
                                        IMediaFileService mediaFileService,
                                        IMoveTrackFiles trackFileMover,
                                        IEventAggregator eventAggregator,
                                        ITrackService trackService,
                                        IBuildFileNames filenameBuilder,
                                        IDiskProvider diskProvider,
                                        Logger logger)
        {
            _artistService = artistService;
            _albumService = albumService;
            _mediaFileService = mediaFileService;
            _trackFileMover = trackFileMover;
            _eventAggregator = eventAggregator;
            _trackService = trackService;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<RenameTrackFilePreview> GetRenamePreviews(int artistId)
        {
            // TODO
            throw new NotImplementedException();
            //var artist = _artistService.GetArtist(artistId);
            //var tracks = _trackService.GetTracksByArtist(artistId);
            //var files = _mediaFileService.GetFilesByArtist(artistId);

            //return GetPreviews(artist, tracks, files)
            //    .OrderByDescending(e => e.SeasonNumber)
            //    .ThenByDescending(e => e.TrackNumbers.First())
            //    .ToList();
        }

        public List<RenameTrackFilePreview> GetRenamePreviews(int artistId, int albumId)
        {
            // TODO
            //throw new NotImplementedException();
            var artist = _artistService.GetArtist(artistId);
            var album = _albumService.GetAlbum(albumId);
            var tracks = _trackService.GetTracksByAlbum(artistId, albumId);
            var files = _mediaFileService.GetFilesByAlbum(artistId, albumId);

            return GetPreviews(artist, album, tracks, files)
                .OrderByDescending(e => e.TrackNumbers.First()).ToList();
        }

        private IEnumerable<RenameTrackFilePreview> GetPreviews(Artist artist, Album album, List<Track> tracks, List<TrackFile> files)
        {
            foreach (var f in files)
            {
                var file = f;
                var tracksInFile = tracks.Where(e => e.TrackFileId == file.Id).ToList();
                var trackFilePath = Path.Combine(artist.Path, file.RelativePath);

                if (!tracksInFile.Any())
                {
                    _logger.Warn("File ({0}) is not linked to any tracks", trackFilePath);
                    continue;
                }

                var albumId = tracksInFile.First().AlbumId;
                var newName = _filenameBuilder.BuildTrackFileName(tracksInFile, artist, album, file);
                var newPath = _filenameBuilder.BuildTrackFilePath(artist, album, newName, Path.GetExtension(trackFilePath));

                if (!trackFilePath.PathEquals(newPath, StringComparison.Ordinal))
                {
                    yield return new RenameTrackFilePreview
                    {
                        ArtistId = artist.Id,
                        AlbumId = albumId,
                        TrackNumbers = tracksInFile.Select(e => e.TrackNumber).ToList(),
                        TrackFileId = file.Id,
                        ExistingPath = file.RelativePath,
                        NewPath = artist.Path.GetRelativePath(newPath)
                    };
                }
            }
        }

        private void RenameFiles(List<TrackFile> trackFiles, Artist artist)
        {
            // TODO
            throw new NotImplementedException();
            //var renamed = new List<TrackFile>();

            //foreach (var trackFile in trackFiles)
            //{
            //    var trackFilePath = Path.Combine(artist.Path, trackFile.RelativePath);

            //    try
            //    {
            //        _logger.Debug("Renaming track file: {0}", trackFile);
            //        _trackFileMover.MoveTrackFile(trackFile, artist);

            //        _mediaFileService.Update(trackFile);
            //        renamed.Add(trackFile);

            //        _logger.Debug("Renamed track file: {0}", trackFile);
            //    }
            //    catch (SameFilenameException ex)
            //    {
            //        _logger.Debug("File not renamed, source and destination are the same: {0}", ex.Filename);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.Error(ex, "Failed to rename file {0}", trackFilePath);
            //    }
            //}

            //if (renamed.Any())
            //{
            //    _diskProvider.RemoveEmptySubfolders(artist.Path);

            //    _eventAggregator.PublishEvent(new ArtistRenamedEvent(artist));
            //}
        }

        public void Execute(RenameFilesCommand message)
        {
            // TODO
            throw new NotImplementedException();
            //var artist = _artistService.GetArtist(message.ArtistId);
            //var trackFiles = _mediaFileService.Get(message.Files);

            //_logger.ProgressInfo("Renaming {0} files for {1}", trackFiles.Count, artist.Title);
            //RenameFiles(trackFiles, artist);
            //_logger.ProgressInfo("Selected track files renamed for {0}", artist.Title);
        }

        public void Execute(RenameArtistCommand message)
        {
            // TODO
            throw new NotImplementedException();
            //_logger.Debug("Renaming all files for selected artist");
            //var artistToRename = _artistService.GetArtist(message.ArtistIds);

            //foreach (var artist in artistToRename)
            //{
            //    var trackFiles = _mediaFileService.GetFilesByArtist(artist.Id);
            //    _logger.ProgressInfo("Renaming all files in artist: {0}", artist.Title);
            //    RenameFiles(trackFiles, artist);
            //    _logger.ProgressInfo("All track files renamed for {0}", artist.Title);
            //}
        }
    }
}
