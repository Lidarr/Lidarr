using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(082)]
    public class add_track_monitoring : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Tracks").AddColumn("Monitored").AsBoolean().NotNullable().WithDefaultValue(true);
            Create.Index().OnTable("Tracks").OnColumn("Monitored");
        }
    }
}
