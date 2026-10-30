using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Music
{
    public interface ITrackRepository : IBasicRepository<Track>
    {
        List<Track> GetTracks(int artistId);
        List<Track> GetTracksByAlbum(int albumId);
        List<Track> GetTracksByRelease(int albumReleaseId);
        List<Track> GetTracksByReleases(List<int> albumReleaseIds);
        List<Track> GetTracksForRefresh(int albumReleaseId, List<string> foreignTrackIds);
        List<Track> GetTracksByFileId(int fileId);
        List<Track> GetTracksByFileId(IEnumerable<int> ids);
        List<Track> TracksWithFiles(int artistId);
        List<Track> TracksWithoutFiles(int albumId);
        void SetFileId(List<Track> tracks);
        void DetachTrackFile(int trackFileId);
        void LoadReleasesAndArtists(List<Track> tracks);
    }

    public class TrackRepository : BasicRepository<Track>, ITrackRepository
    {
        public TrackRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Track> GetTracks(int artistId)
        {
            return Query(Builder()
                         .Join<Track, AlbumRelease>((t, r) => t.AlbumReleaseId == r.Id)
                         .Join<AlbumRelease, Album>((r, a) => r.AlbumId == a.Id)
                         .Join<Album, Artist>((album, artist) => album.ArtistMetadataId == artist.ArtistMetadataId)
                         .Where<AlbumRelease>(r => r.Monitored == true)
                         .Where<Artist>(x => x.Id == artistId));
        }

        public List<Track> GetTracksByAlbum(int albumId)
        {
            return Query(Builder()
                         .Join<Track, AlbumRelease>((t, r) => t.AlbumReleaseId == r.Id)
                         .Join<AlbumRelease, Album>((r, a) => r.AlbumId == a.Id)
                         .Where<AlbumRelease>(r => r.Monitored == true)
                         .Where<Album>(x => x.Id == albumId));
        }

        public List<Track> GetTracksByRelease(int albumReleaseId)
        {
            return Query(t => t.AlbumReleaseId == albumReleaseId).ToList();
        }

        public List<Track> GetTracksByReleases(List<int> albumReleaseIds)
        {
            // this will populate the artist metadata also
            return _database.QueryJoined<Track, ArtistMetadata>(Builder()
                               .Join<Track, ArtistMetadata>((l, r) => l.ArtistMetadataId == r.Id)
                               .Where<Track>(x => albumReleaseIds.Contains(x.AlbumReleaseId)),
                (track, metadata) =>
                    {
                        track.ArtistMetadata = metadata;
                        return track;
                    }).ToList();
        }

        public List<Track> GetTracksForRefresh(int albumReleaseId, List<string> foreignTrackIds)
        {
            return Query(a => a.AlbumReleaseId == albumReleaseId || foreignTrackIds.Contains(a.ForeignTrackId));
        }

        public List<Track> GetTracksByFileId(int fileId)
        {
            return Query(e => e.TrackFileId == fileId);
        }

        public List<Track> GetTracksByFileId(IEnumerable<int> ids)
        {
            return Query(x => ids.Contains(x.TrackFileId));
        }

        public List<Track> TracksWithFiles(int artistId)
        {
            return Query(Builder()
                         .Join<Track, AlbumRelease>((t, r) => t.AlbumReleaseId == r.Id)
                         .Join<AlbumRelease, Album>((r, a) => r.AlbumId == a.Id)
                         .Join<Album, Artist>((album, artist) => album.ArtistMetadataId == artist.ArtistMetadataId)
                         .Join<Track, TrackFile>((t, f) => t.TrackFileId == f.Id)
                         .Where<AlbumRelease>(r => r.Monitored == true)
                         .Where<Artist>(x => x.Id == artistId));
        }

        public List<Track> TracksWithoutFiles(int albumId)
        {
            // x.Id == null is converted to SQL, so warning incorrect
#pragma warning disable CS0472
            return Query(Builder()
                         .Join<Track, AlbumRelease>((t, r) => t.AlbumReleaseId == r.Id)
                         .LeftJoin<Track, TrackFile>((t, f) => t.TrackFileId == f.Id)
                         .Where<AlbumRelease>(r => r.Monitored == true && r.AlbumId == albumId)
                         .Where<TrackFile>(x => x.Id == null));
#pragma warning restore CS0472
        }

        public void SetFileId(List<Track> tracks)
        {
            SetFields(tracks, t => t.TrackFileId);
        }

        public void DetachTrackFile(int trackFileId)
        {
            var tracks = GetTracksByFileId(trackFileId);
            tracks.ForEach(x => x.TrackFileId = 0);
            SetFileId(tracks);
        }

        public void LoadReleasesAndArtists(List<Track> tracks)
        {
            var withoutRelease = tracks.Where(t => t.AlbumReleaseId > 0 && (t.AlbumRelease == null || !t.AlbumRelease.IsLoaded)).ToList();

            if (withoutRelease.Any())
            {
                var releaseIds = withoutRelease.Select(t => t.AlbumReleaseId).Distinct().ToList();
                var releases = _database.Query<AlbumRelease>(new SqlBuilder(_database.DatabaseType).Where<AlbumRelease>(r => releaseIds.Contains(r.Id)))
                    .ToDictionary(r => r.Id);

                var albumIds = releases.Values.Where(r => r.AlbumId > 0).Select(r => r.AlbumId).Distinct().ToList();
                var albums = albumIds.Any()
                    ? _database.Query<Album>(new SqlBuilder(_database.DatabaseType).Where<Album>(a => albumIds.Contains(a.Id))).ToDictionary(a => a.Id)
                    : new Dictionary<int, Album>();

                foreach (var release in releases.Values.Where(r => r.AlbumId > 0))
                {
                    release.Album = albums.GetValueOrDefault(release.AlbumId);
                }

                foreach (var track in withoutRelease)
                {
                    track.AlbumRelease = releases.GetValueOrDefault(track.AlbumReleaseId);
                }
            }

            var withoutArtist = tracks.Where(t => t.Id > 0 && (t.Artist == null || !t.Artist.IsLoaded)).ToList();

            if (withoutArtist.Any())
            {
                var releaseIds = withoutArtist.Select(t => t.AlbumReleaseId).Distinct().ToList();
                var artistByRelease = new Dictionary<int, Artist>();

                _database.QueryJoined<Artist, ArtistMetadata, AlbumRelease>(
                    new SqlBuilder(_database.DatabaseType)
                        .Join<Artist, ArtistMetadata>((a, m) => a.ArtistMetadataId == m.Id)
                        .Join<Artist, Album>((l, r) => l.ArtistMetadataId == r.ArtistMetadataId)
                        .Join<Album, AlbumRelease>((l, r) => l.Id == r.AlbumId)
                        .Where<AlbumRelease>(r => releaseIds.Contains(r.Id)),
                    (artist, metadata, release) =>
                    {
                        artist.Metadata = metadata;
                        artistByRelease[release.Id] = artist;
                        return artist;
                    }).ToList();

                foreach (var track in withoutArtist)
                {
                    track.Artist = artistByRelease.GetValueOrDefault(track.AlbumReleaseId);
                }
            }
        }
    }
}
