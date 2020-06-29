using FluentMigrator;

using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(29)]
    public class health_issue_notification : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnHealthIssue").AsInt32().WithDefaultValue(0);
            Alter.Table("Notifications").AddColumn("IncludeHealthWarnings").AsInt32().WithDefaultValue(0);
            Alter.Table("Notifications").AddColumn("OnDownloadFailure").AsInt32().WithDefaultValue(0);
            Alter.Table("Notifications").AddColumn("OnImportFailure").AsInt32().WithDefaultValue(0);
            Alter.Table("Notifications").AddColumn("OnTrackRetag").AsInt32().WithDefaultValue(0);

            Delete.Column("OnDownload").FromTable("Notifications");

            Rename.Column("OnAlbumDownload").OnTable("Notifications").To("OnReleaseImport");
        }
    }
}
