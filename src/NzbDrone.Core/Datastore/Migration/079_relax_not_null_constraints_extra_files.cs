using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(079)]
    public class relax_not_null_constraints_extra_files : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("ExtraFiles").AlterColumn("TrackFileId").AsInt32().Nullable();
        }
    }
}
