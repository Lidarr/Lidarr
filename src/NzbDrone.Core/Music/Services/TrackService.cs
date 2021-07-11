using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Music
{
    public interface ITrackService
    {
        Track GetTrack(int id);
        Track FindTrackByTitleInexact(string foreignAlbumId, string title);
        List<Track> GetTracks(IEnumerable<int> ids);
        List<Track> GetTracksByArtist(int artistId);
        List<Track> GetTracksByAlbum(int albumId);
        List<Track> GetTracksByRelease(int albumReleaseId);
        List<Track> GetTracksByReleases(List<int> albumReleaseIds);
        List<Track> GetTracksForRefresh(int albumReleaseId, IEnumerable<string> foreignTrackIds);
        List<Track> TracksWithFiles(int artistId);
        List<Track> TracksWithoutFiles(int albumId);
        List<Track> GetTracksByFileId(int trackFileId);
        List<Track> GetTracksByFileId(IEnumerable<int> trackFileIds);
        void UpdateTrack(Track track);
        void InsertMany(List<Track> tracks);
        void UpdateMany(List<Track> tracks);
        void DeleteMany(List<Track> tracks);
        void SetFileIds(List<Track> tracks);
    }

    public class TrackService : ITrackService,
                                IHandle<ReleaseDeletedEvent>,
                                IHandle<TrackFileDeletedEvent>
    {
        private readonly ITrackRepository _trackRepository;
        private readonly Logger _logger;

        public TrackService(ITrackRepository trackRepository,
                            Logger logger)
        {
            _trackRepository = trackRepository;
            _logger = logger;
        }

        public Track GetTrack(int id)
        {
            return _trackRepository.Get(id);
        }

        public Track FindTrackByTitleInexact(string foreignAlbumId, string title)
        {
            var normalizedTitle = title.NormalizeTrackTitle().Replace(".", " ");
            var tracks = _trackRepository.GetTracksByForeignAlbumId(foreignAlbumId);

            Func<Func<Track, string, double>, string, Tuple<Func<Track, string, double>, string>> tc = Tuple.Create;
            var scoringFunctions = new List<Tuple<Func<Track, string, double>, string>>
            {
                tc((a, t) => a.Title.NormalizeTrackTitle().FuzzyMatch(t), normalizedTitle),
                tc((a, t) => a.Title.NormalizeTrackTitle().FuzzyContains(t), normalizedTitle),
                tc((a, t) => t.FuzzyContains(a.Title.NormalizeTrackTitle()), normalizedTitle)
            };

            foreach (var func in scoringFunctions)
            {
                var track = FindByStringInexact(tracks, func.Item1, func.Item2);
                if (track != null)
                {
                    return track;
                }
            }

            return null;
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

        public List<Track> GetTracksForRefresh(int albumReleaseId, IEnumerable<string> foreignTrackIds)
        {
            return _trackRepository.GetTracksForRefresh(albumReleaseId, foreignTrackIds);
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

        public List<Track> GetTracksByFileId(IEnumerable<int> trackFileIds)
        {
            return _trackRepository.GetTracksByFileId(trackFileIds);
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

        public void SetFileIds(List<Track> tracks)
        {
            _trackRepository.SetFileId(tracks);
        }

        public void Handle(ReleaseDeletedEvent message)
        {
            var tracks = GetTracksByRelease(message.Release.Id);
            _trackRepository.DeleteMany(tracks);
        }

        public void Handle(TrackFileDeletedEvent message)
        {
            _logger.Debug($"Detaching tracks from file {message.TrackFile}");
            _trackRepository.DetachTrackFile(message.TrackFile.Id);
        }

        private Track FindByStringInexact(List<Track> tracks, Func<Track, string, double> scoreFunction, string title)
        {
            const double fuzzThreshold = 0.7;
            const double fuzzGap = 0.2;

            var sortedTracks = tracks.Select(s => new
            {
                MatchProb = scoreFunction(s, title),
                Track = s
            }).ToList()
                .OrderByDescending(s => s.MatchProb)
                .ToList();

            if (!sortedTracks.Any())
            {
                return null;
            }

            if (sortedTracks[0].MatchProb > fuzzThreshold
                && (sortedTracks.Count == 1 || sortedTracks[0].MatchProb - sortedTracks[1].MatchProb > fuzzGap))
            {
                return sortedTracks[0].Track;
            }

            return null;
        }
    }
}
