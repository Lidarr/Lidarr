using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(58)]
    public class ImportListMonitorExisting : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("ImportLists").AddColumn("ShouldMonitorExisting").AsInt32().WithDefaultValue(0);
        }
    }
}
