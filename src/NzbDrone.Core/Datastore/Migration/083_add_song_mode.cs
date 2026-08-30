using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(083)]
    public class add_song_mode : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Artists").AddColumn("SongMode").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
