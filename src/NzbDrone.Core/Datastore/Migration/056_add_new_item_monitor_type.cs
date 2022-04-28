using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(56)]
    public class AddNewItemMonitorType : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Artists").AddColumn("MonitorNewItems").AsInt32().WithDefaultValue(0);
            Alter.Table("RootFolders").AddColumn("DefaultNewItemMonitorOption").AsInt32().WithDefaultValue(0);
            Alter.Table("ImportLists").AddColumn("MonitorNewItems").AsInt32().WithDefaultValue(0);
        }
    }
}
