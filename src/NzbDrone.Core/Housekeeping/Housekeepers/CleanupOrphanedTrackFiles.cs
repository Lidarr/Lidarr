using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedTrackFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedTrackFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            CleanupOrphanedByAlbum();
            CleanupOrphanedByTracks();
        }

        private void CleanupOrphanedByAlbum()
        {
            using (var mapper = _database.OpenConnection())
            {
                // Unlink where track no longer exists
                mapper.Execute(@"UPDATE ""TrackFiles""
                                     SET ""AlbumId"" = 0
                                     WHERE ""AlbumId"" <> 0 AND  ""Id"" IN (
                                     SELECT ""TrackFiles"".""Id"" FROM ""TrackFiles""
                                     LEFT OUTER JOIN ""Tracks""
                                     ON ""TrackFiles"".""Id"" = ""Tracks"".""TrackFileId""
                                     WHERE ""Tracks"".""Id"" IS NULL)");
            }
        }

        private void CleanupOrphanedByTracks()
        {
            using (var mapper = _database.OpenConnection())
            {
                // Unlink Tracks where the Trackfiles entry no longer exists
                mapper.Execute(@"UPDATE ""Tracks""
                                     SET ""TrackFileId"" = 0
                                     WHERE ""TrackFileId"" <> 0 AND ""Id"" IN (
                                     SELECT ""Tracks"".""Id"" FROM ""Tracks""
                                     LEFT OUTER JOIN ""TrackFiles""
                                     ON ""Tracks"".""TrackFileId"" = ""TrackFiles"".""Id""
                                     WHERE ""TrackFiles"".""Id"" IS NULL)");
            }
        }
    }
}
