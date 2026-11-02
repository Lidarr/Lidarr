using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(082)]
    public class add_album_release_filter : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Albums").AddColumn("PreferredTrackCounts").AsString().WithDefaultValue("[]");
            Alter.Table("Albums").AddColumn("PreferredMediumCounts").AsString().WithDefaultValue("[]");
        }
    }
}
