using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.ArtistStats
{
    public interface IArtistStatisticsRepository
    {
        List<AlbumStatistics> ArtistStatistics();
        List<AlbumStatistics> ArtistStatistics(int artistId);
    }

    public class ArtistStatisticsRepository : IArtistStatisticsRepository
    {
        private const string _selectTracksTemplate = "SELECT /**select**/ FROM \"Tracks\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";
        private const string _selectTrackFilesTemplate = "SELECT /**select**/ FROM \"TrackFiles\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";

        private readonly IMainDatabase _database;

        public ArtistStatisticsRepository(IMainDatabase database)
        {
            _database = database;
        }

        public List<AlbumStatistics> ArtistStatistics()
        {
            var time = DateTime.UtcNow;
            return MapResults(Query(TracksBuilder(time), _selectTracksTemplate),
                Query(TrackFilesBuilder(), _selectTrackFilesTemplate));
        }

        public List<AlbumStatistics> ArtistStatistics(int artistId)
        {
            var time = DateTime.UtcNow;

            return MapResults(Query(TracksBuilder(time).Where<Artist>(x => x.Id == artistId), _selectTracksTemplate),
                Query(TrackFilesBuilder().Where<Artist>(x => x.Id == artistId), _selectTrackFilesTemplate));
        }

        private List<AlbumStatistics> MapResults(List<AlbumStatistics> tracksResult, List<AlbumStatistics> filesResult)
        {
            tracksResult.ForEach(e =>
            {
                var file = filesResult.SingleOrDefault(f => f.ArtistId == e.ArtistId & f.AlbumId == e.AlbumId);

                e.SizeOnDisk = file?.SizeOnDisk ?? 0;
            });

            return tracksResult;
        }

        private List<AlbumStatistics> Query(SqlBuilder builder, string template)
        {
            var sql = builder.AddTemplate(template).LogQuery();

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<AlbumStatistics>(sql.RawSql, sql.Parameters).ToList();
            }
        }

        private SqlBuilder TracksBuilder(DateTime currentDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("currentDate", currentDate, null);

            var trueIndicator = _database.DatabaseType == DatabaseType.PostgreSQL ? "true" : "1";

            return new SqlBuilder(_database.DatabaseType)
                .Select($@"""Artists"".""Id"" AS ""ArtistId"",
                        ""Albums"".""Id"" AS ""AlbumId"",
                        COUNT(""Tracks"".""Id"") AS ""TotalTrackCount"",
                        SUM(CASE WHEN ""Albums"".""ReleaseDate"" <= @currentDate OR ""Tracks"".""TrackFileId"" > 0 THEN 1 ELSE 0 END) AS ""AvailableTrackCount"",
                        SUM(CASE WHEN (""Albums"".""Monitored"" = {trueIndicator} AND ""Albums"".""ReleaseDate"" <= @currentDate) OR ""Tracks"".""TrackFileId"" > 0 THEN 1 ELSE 0 END) AS ""TrackCount"",
                        SUM(CASE WHEN ""Tracks"".""TrackFileId"" > 0 THEN 1 ELSE 0 END) AS TrackFileCount", parameters)
                .Join<Track, AlbumRelease>((t, r) => t.AlbumReleaseId == r.Id)
                .Join<AlbumRelease, Album>((r, a) => r.AlbumId == a.Id)
                .Join<Album, Artist>((album, artist) => album.ArtistMetadataId == artist.ArtistMetadataId)
                .Where<AlbumRelease>(x => x.Monitored == true)
                .GroupBy<Artist>(x => x.Id)
                .GroupBy<Album>(x => x.Id);
        }

        private SqlBuilder TrackFilesBuilder()
        {
            return new SqlBuilder(_database.DatabaseType)
                .Select(@"""Artists"".""Id"" AS ""ArtistId"",
                        ""AlbumId"",
                        SUM(COALESCE(""Size"", 0)) AS SizeOnDisk")
                .Join<TrackFile, Album>((t, a) => t.AlbumId == a.Id)
                .Join<Album, Artist>((album, artist) => album.ArtistMetadataId == artist.ArtistMetadataId)
                .GroupBy<Artist>(x => x.Id)
                .GroupBy<TrackFile>(x => x.AlbumId);
        }
    }
}
