using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Common;
using NzbDrone.Core.Music;
using System;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileService
    {
        TrackFile Add(TrackFile trackFile);
        void AddMany(List<TrackFile> trackFiles);
        void Update(TrackFile trackFile);
        void Update(List<TrackFile> trackFile);
        void Delete(TrackFile trackFile, DeleteMediaFileReason reason);
        List<TrackFile> GetFilesByArtist(int artistId);
        List<TrackFile> GetFilesByAlbum(int albumId);
        List<TrackFile> GetFilesByRelease(int releaseId);
        List<string> FilterExistingFiles(List<string> files, Artist artist);
        TrackFile Get(int id);
        List<TrackFile> Get(IEnumerable<int> ids);
        List<TrackFile> GetFilesWithRelativePath(int artistId, string relativePath);
        void UpdateMediaInfo(List<TrackFile> trackFiles);
    }

    public class MediaFileService : IMediaFileService, IHandleAsync<AlbumDeletedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly Logger _logger;

        public MediaFileService(IMediaFileRepository mediaFileRepository, IEventAggregator eventAggregator, Logger logger)
        {
            _mediaFileRepository = mediaFileRepository;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public TrackFile Add(TrackFile trackFile)
        {
            var addedFile = _mediaFileRepository.Insert(trackFile);
            _eventAggregator.PublishEvent(new TrackFileAddedEvent(addedFile));
            return addedFile;
        }

        public void AddMany(List<TrackFile> trackFiles)
        {
            _mediaFileRepository.InsertMany(trackFiles);
            foreach (var addedFile in trackFiles)
            {
                _eventAggregator.PublishEvent(new TrackFileAddedEvent(addedFile));
            }
        }

        public void Update(TrackFile trackFile)
        {
            _mediaFileRepository.Update(trackFile);
        }

        public void Update(List<TrackFile> trackFiles)
        {
            _mediaFileRepository.UpdateMany(trackFiles);
        }


        public void Delete(TrackFile trackFile, DeleteMediaFileReason reason)
        {
            trackFile.Path = Path.Combine(trackFile.Artist.Value.Path, trackFile.RelativePath);

            _mediaFileRepository.Delete(trackFile);
            _eventAggregator.PublishEvent(new TrackFileDeletedEvent(trackFile, reason));
        }

        public List<string> FilterExistingFiles(List<string> files, Artist artist)
        {
            var artistFiles = GetFilesByArtist(artist.Id).Select(f => Path.Combine(artist.Path, f.RelativePath)).ToList();

            if (!artistFiles.Any()) return files;

            return files.Except(artistFiles, PathEqualityComparer.Instance).ToList();
        }

        public TrackFile Get(int id)
        {
            return _mediaFileRepository.Get(id);
        }

        public List<TrackFile> Get(IEnumerable<int> ids)
        {
            return _mediaFileRepository.Get(ids).ToList();
        }

        public List<TrackFile> GetFilesWithRelativePath(int artistId, string relativePath)
        {
            return _mediaFileRepository.GetFilesWithRelativePath(artistId, relativePath);
        }

        public void HandleAsync(AlbumDeletedEvent message)
        {
            var files = GetFilesByAlbum(message.Album.Id);
            _mediaFileRepository.DeleteMany(files);
        }

        public List<TrackFile> GetFilesByArtist(int artistId)
        {
            return _mediaFileRepository.GetFilesByArtist(artistId);
        }

        public List<TrackFile> GetFilesByAlbum(int albumId)
        {
            return _mediaFileRepository.GetFilesByAlbum(albumId);
        }

        public List<TrackFile> GetFilesByRelease(int releaseId)
        {
            return _mediaFileRepository.GetFilesByRelease(releaseId);
        }

        public void UpdateMediaInfo(List<TrackFile> trackFiles)
        {
            _mediaFileRepository.SetFields(trackFiles, t => t.MediaInfo);
        }
    }
}
