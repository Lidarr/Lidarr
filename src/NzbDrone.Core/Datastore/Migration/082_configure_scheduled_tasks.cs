using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(82)]
    public class configure_scheduled_tasks : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("ScheduledTasks")
                .AddColumn("IsUserConfigured").AsBoolean().WithDefaultValue(false);

            Execute.Sql(@"UPDATE ""ScheduledTasks""
                         SET ""Interval"" = (SELECT CASE WHEN CAST(""Value"" AS INTEGER) < 1 THEN 1440 WHEN CAST(""Value"" AS INTEGER) > 7 THEN 10080 ELSE CAST(""Value"" AS INTEGER) * 1440 END FROM ""Config"" WHERE ""Key"" = 'backupinterval'),
                             ""IsUserConfigured"" = 1
                         WHERE ""TypeName"" = 'NzbDrone.Core.Backup.BackupCommand'
                           AND EXISTS (SELECT 1 FROM ""Config"" WHERE ""Key"" = 'backupinterval')");

            Delete.FromTable("Config").Row(new { Key = "backupinterval" });

            Execute.Sql(@"UPDATE ""ScheduledTasks""
                        SET ""Interval"" = (SELECT CASE WHEN CAST(""Value"" AS INTEGER) > 0 AND CAST(""Value"" AS INTEGER) < 10 THEN 10 WHEN CAST(""Value"" AS INTEGER) < 0 THEN 0 ELSE CAST(""Value"" AS INTEGER) END FROM ""Config"" WHERE ""Key"" = 'rsssyncinterval'),
                                ""IsUserConfigured"" = 1
                        WHERE ""TypeName"" = 'NzbDrone.Core.Indexers.RssSyncCommand'
                            AND EXISTS (SELECT 1 FROM ""Config"" WHERE ""Key"" = 'rsssyncinterval')");
        }
    }
}
