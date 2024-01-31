using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(076)]
    public class add_on_artist_add_to_notifications : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnArtistAdd").AsBoolean().WithDefaultValue(false);
        }
    }
}
