using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(061)]
    public class postgres_update_timestamp_columns_to_with_timezone : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.FromTable("Commands").AllRows();

            Alter.Table("AlbumReleases").AlterColumn("ReleaseDate").AsDateTimeOffset().Nullable();
            Alter.Table("Albums").AlterColumn("LastInfoSync").AsDateTimeOffset().Nullable();
            Alter.Table("Albums").AlterColumn("ReleaseDate").AsDateTimeOffset().Nullable();
            Alter.Table("Albums").AlterColumn("Added").AsDateTimeOffset().Nullable();
            Alter.Table("Artists").AlterColumn("LastInfoSync").AsDateTimeOffset().Nullable();
            Alter.Table("Artists").AlterColumn("Added").AsDateTimeOffset().Nullable();
            Alter.Table("Blocklist").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            Alter.Table("Blocklist").AlterColumn("PublishedDate").AsDateTimeOffset().Nullable();
            Alter.Table("Commands").AlterColumn("QueuedAt").AsDateTimeOffset().NotNullable();
            Alter.Table("Commands").AlterColumn("StartedAt").AsDateTimeOffset().Nullable();
            Alter.Table("Commands").AlterColumn("EndedAt").AsDateTimeOffset().Nullable();
            Alter.Table("DownloadClientStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            Alter.Table("DownloadClientStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            Alter.Table("DownloadClientStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            Alter.Table("DownloadHistory").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            Alter.Table("ExtraFiles").AlterColumn("Added").AsDateTimeOffset().NotNullable();
            Alter.Table("ExtraFiles").AlterColumn("LastUpdated").AsDateTimeOffset().NotNullable();
            Alter.Table("History").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            Alter.Table("ImportListStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            Alter.Table("ImportListStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            Alter.Table("ImportListStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            Alter.Table("IndexerStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            Alter.Table("IndexerStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            Alter.Table("IndexerStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            Alter.Table("LyricFiles").AlterColumn("LastUpdated").AsDateTimeOffset().NotNullable();
            Alter.Table("LyricFiles").AlterColumn("Added").AsDateTimeOffset().Nullable();
            Alter.Table("MetadataFiles").AlterColumn("LastUpdated").AsDateTimeOffset().NotNullable();
            Alter.Table("MetadataFiles").AlterColumn("Added").AsDateTimeOffset().Nullable();
            Alter.Table("PendingReleases").AlterColumn("Added").AsDateTimeOffset().NotNullable();
            Alter.Table("ScheduledTasks").AlterColumn("LastExecution").AsDateTimeOffset().NotNullable();
            Alter.Table("ScheduledTasks").AlterColumn("LastStartTime").AsDateTimeOffset().Nullable();
            Alter.Table("TrackFiles").AlterColumn("DateAdded").AsDateTimeOffset().NotNullable();
            Alter.Table("TrackFiles").AlterColumn("Modified").AsDateTimeOffset().NotNullable();
            Alter.Table("VersionInfo").AlterColumn("AppliedOn").AsDateTimeOffset().Nullable();
        }

        protected override void LogDbUpgrade()
        {
            Alter.Table("Logs").AlterColumn("Time").AsDateTimeOffset().NotNullable();
            Alter.Table("UpdateHistory").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            Alter.Table("VersionInfo").AlterColumn("AppliedOn").AsDateTimeOffset().Nullable();
        }
    }
}
