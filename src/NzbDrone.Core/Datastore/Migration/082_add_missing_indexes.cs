using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(082)]
    public class add_missing_indexes : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Index().OnTable("Tracks").OnColumn("AlbumReleaseId");
            Create.Index().OnTable("Tracks").OnColumn("AlbumReleaseId").Ascending()
                                            .OnColumn("TrackFileId").Ascending();

            Create.Index().OnTable("History").OnColumn("ArtistId").Ascending()
                                             .OnColumn("AlbumId").Ascending();

            Delete.Index().OnTable("History").OnColumn("EventType");
            Create.Index().OnTable("History").OnColumn("EventType").Ascending()
                                             .OnColumn("Date").Ascending();

            Create.Index().OnTable("Blocklist").OnColumn("ArtistId");
            Create.Index().OnTable("PendingReleases").OnColumn("ArtistId");

            Create.Index().OnTable("ExtraFiles").OnColumn("ArtistId").Ascending()
                                                .OnColumn("AlbumId").Ascending();
            Create.Index().OnTable("ExtraFiles").OnColumn("TrackFileId");

            Create.Index().OnTable("LyricFiles").OnColumn("ArtistId").Ascending()
                                                .OnColumn("AlbumId").Ascending();
            Create.Index().OnTable("LyricFiles").OnColumn("TrackFileId");

            Create.Index().OnTable("MetadataFiles").OnColumn("ArtistId").Ascending()
                                                   .OnColumn("AlbumId").Ascending();
            Create.Index().OnTable("MetadataFiles").OnColumn("TrackFileId");
        }
    }
}
