using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryRepository : IBasicRepository<EntityHistory>
    {
        EntityHistory MostRecentForAlbum(int albumId);
        EntityHistory MostRecentForDownloadId(string downloadId);
        List<EntityHistory> FindByDownloadId(string downloadId);
        List<EntityHistory> GetByArtist(int artistId, EntityHistoryEventType? eventType);
        List<EntityHistory> GetByAlbum(int albumId, EntityHistoryEventType? eventType);
        List<EntityHistory> FindDownloadHistory(int idArtistId, QualityModel quality);
        void DeleteForArtists(List<int> artistIds);
        List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType);
        PagingSpec<EntityHistory> GetPaged(PagingSpec<EntityHistory> pagingSpec, int[] qualities);
    }

    public class EntityHistoryRepository : BasicRepository<EntityHistory>, IHistoryRepository
    {
        public EntityHistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public EntityHistory MostRecentForAlbum(int albumId)
        {
            return Query(h => h.AlbumId == albumId).MaxBy(h => h.Date);
        }

        public EntityHistory MostRecentForDownloadId(string downloadId)
        {
            return Query(h => h.DownloadId == downloadId).MaxBy(h => h.Date);
        }

        public List<EntityHistory> FindByDownloadId(string downloadId)
        {
            return _database.QueryJoined<EntityHistory, Artist, Album>(
                Builder()
                .Join<EntityHistory, Artist>((h, a) => h.ArtistId == a.Id)
                .Join<EntityHistory, Album>((h, a) => h.AlbumId == a.Id)
                .Where<EntityHistory>(h => h.DownloadId == downloadId),
                (history, artist, album) =>
                {
                    history.Artist = artist;
                    history.Album = album;
                    return history;
                }).ToList();
        }

        public List<EntityHistory> GetByArtist(int artistId, EntityHistoryEventType? eventType)
        {
            var builder = Builder().Where<EntityHistory>(h => h.ArtistId == artistId);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return Query(builder).OrderByDescending(h => h.Date).ToList();
        }

        public List<EntityHistory> GetByAlbum(int albumId, EntityHistoryEventType? eventType)
        {
            var builder = Builder()
                .Join<EntityHistory, Album>((h, a) => h.AlbumId == a.Id)
                .Where<EntityHistory>(h => h.AlbumId == albumId);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return _database.QueryJoined<EntityHistory, Album>(
                builder,
                (history, album) =>
                {
                    history.Album = album;
                    return history;
                }).OrderByDescending(h => h.Date).ToList();
        }

        public List<EntityHistory> FindDownloadHistory(int idArtistId, QualityModel quality)
        {
            var allowed = new[] { (int)EntityHistoryEventType.Grabbed, (int)EntityHistoryEventType.DownloadFailed, (int)EntityHistoryEventType.TrackFileImported };

            return Query(h => h.ArtistId == idArtistId &&
                         h.Quality == quality &&
                         allowed.Contains((int)h.EventType));
        }

        public void DeleteForArtists(List<int> artistIds)
        {
            Delete(c => artistIds.Contains(c.ArtistId));
        }

        public List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType)
        {
            var builder = Builder()
                .Join<EntityHistory, Artist>((h, a) => h.ArtistId == a.Id)
                .Join<EntityHistory, Album>((h, a) => h.AlbumId == a.Id)
                .LeftJoin<EntityHistory, Track>((h, t) => h.TrackId == t.Id)
                .Where<EntityHistory>(x => x.Date >= date);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return _database.QueryJoined<EntityHistory, Artist, Album, Track>(builder, (history, artist, album, track) =>
            {
                history.Artist = artist;
                history.Album = album;
                history.Track = track;
                return history;
            }).OrderBy(h => h.Date).ToList();
        }

        public PagingSpec<EntityHistory> GetPaged(PagingSpec<EntityHistory> pagingSpec, int[] qualities)
        {
            pagingSpec.Records = GetPagedRecords(PagedBuilder(qualities), pagingSpec, PagedQuery);

            var countTemplate = $"SELECT COUNT(*) FROM (SELECT /**select**/ FROM \"{TableMapping.Mapper.TableNameMapping(typeof(EntityHistory))}\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/) AS \"Inner\"";
            pagingSpec.TotalRecords = GetPagedRecordCount(PagedBuilder(qualities).Select(typeof(EntityHistory)), pagingSpec, countTemplate);

            return pagingSpec;
        }

        private SqlBuilder PagedBuilder(int[] qualities)
        {
            var builder = Builder()
                .Join<EntityHistory, Artist>((h, a) => h.ArtistId == a.Id)
                .Join<EntityHistory, Album>((h, a) => h.AlbumId == a.Id)
                .LeftJoin<EntityHistory, Track>((h, t) => h.TrackId == t.Id);

            if (qualities is { Length: > 0 })
            {
                builder.Where($"({BuildQualityWhereClause(qualities)})");
            }

            return builder;
        }

        protected override IEnumerable<EntityHistory> PagedQuery(SqlBuilder builder) =>
            _database.QueryJoined<EntityHistory, Artist, Album, Track>(builder, (history, artist, album, track) =>
            {
                history.Artist = artist;
                history.Album = album;
                history.Track = track;
                return history;
            });

        private string BuildQualityWhereClause(int[] qualities)
        {
            var clauses = new List<string>();

            foreach (var quality in qualities)
            {
                clauses.Add($"\"{TableMapping.Mapper.TableNameMapping(typeof(EntityHistory))}\".\"Quality\" LIKE '%_quality_: {quality},%'");
            }

            return $"({string.Join(" OR ", clauses)})";
        }
    }
}
