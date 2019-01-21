using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Music
{
    public interface ITrackService
    {
        Track GetTrack(int id);
        List<Track> GetTracks(IEnumerable<int> ids);
        List<Track> GetTracksByArtist(int artistId);
        List<Track> GetTracksByAlbum(int albumId);
        List<Track> GetTracksByRelease(int albumReleaseId);
        List<Track> GetTracksByReleases(List<int> albumReleaseIds);
        List<Track> GetTracksByForeignReleaseId(string foreignReleaseId);
        List<Track> GetTracksByForeignTrackIds(List<string> ids);
        List<Track> TracksWithFiles(int artistId);
        List<Track> TracksWithoutFiles(int albumId);
        List<Track> GetTracksByFileId(int trackFileId);
        void UpdateTrack(Track track);
        void InsertMany(List<Track> tracks);
        void UpdateMany(List<Track> tracks);
        void DeleteMany(List<Track> tracks);
    }

    public class TrackService : ITrackService,
                                IHandleAsync<ReleaseDeletedEvent>,
                                IHandle<TrackFileDeletedEvent>,
                                IHandle<TrackFileAddedEvent>
    {
        private readonly ITrackRepository _trackRepository;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public TrackService(ITrackRepository trackRepository, IConfigService configService, Logger logger)
        {
            _trackRepository = trackRepository;
            _configService = configService;
            _logger = logger;
        }

        public Track GetTrack(int id)
        {
            return _trackRepository.Get(id);
        }

        public List<Track> GetTracks(IEnumerable<int> ids)
        {
            return _trackRepository.Get(ids).ToList();
        }

        public List<Track> GetTracksByArtist(int artistId)
        {
            _logger.Debug("Getting Tracks for ArtistId {0}", artistId);
            return _trackRepository.GetTracks(artistId).ToList();
        }

        public List<Track> GetTracksByAlbum(int albumId)
        {
            return _trackRepository.GetTracksByAlbum(albumId);
        }

        public List<Track> GetTracksByRelease(int albumReleaseId)
        {
            return _trackRepository.GetTracksByRelease(albumReleaseId);
        }

        public List<Track> GetTracksByReleases(List<int> albumReleaseIds)
        {
            return _trackRepository.GetTracksByReleases(albumReleaseIds);
        }

        public List<Track> GetTracksByForeignReleaseId(string foreignReleaseId)
        {
            return _trackRepository.GetTracksByForeignReleaseId(foreignReleaseId);
        }

        public List<Track> GetTracksByForeignTrackIds(List<string> ids)
        {
            return _trackRepository.GetTracksByForeignTrackIds(ids);
        }

        public List<Track> TracksWithFiles(int artistId)
        {
            return _trackRepository.TracksWithFiles(artistId);
        }

        public List<Track> TracksWithoutFiles(int albumId)
        {
            return _trackRepository.TracksWithoutFiles(albumId);
        }

        public List<Track> GetTracksByFileId(int trackFileId)
        {
            return _trackRepository.GetTracksByFileId(trackFileId);
        }

        public void UpdateTrack(Track track)
        {
            _trackRepository.Update(track);
        }

        public void InsertMany(List<Track> tracks)
        {
            _trackRepository.InsertMany(tracks);
        }

        public void UpdateMany(List<Track> tracks)
        {
            _trackRepository.UpdateMany(tracks);
        }

        public void DeleteMany(List<Track> tracks)
        {
            _trackRepository.DeleteMany(tracks);
        }

        public void HandleAsync(ReleaseDeletedEvent message)
        {
            var tracks = GetTracksByRelease(message.Release.Id);
            _trackRepository.DeleteMany(tracks);
        }

        public void Handle(TrackFileDeletedEvent message)
        {
            foreach (var track in GetTracksByFileId(message.TrackFile.Id))
            {
                _logger.Debug("Detaching track {0} from file.", track.Id);
                track.TrackFileId = 0;
                UpdateTrack(track);
            }
        }

        public void Handle(TrackFileAddedEvent message)
        {
            foreach (var track in message.TrackFile.Tracks.Value)
            {
                _trackRepository.SetFileId(track.Id, message.TrackFile.Id);
                _logger.Debug("Linking [{0}] > [{1}]", message.TrackFile.RelativePath, track);
            }
        }
    }
}
