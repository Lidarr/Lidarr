using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(083)]
    public class add_album_auto_blocklist_unwanted_release : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Albums").AddColumn("AutoBlocklistUnwantedRelease").AsBoolean().WithDefaultValue(false);
        }
    }
}
