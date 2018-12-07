using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(077)]
    public class album_last_searched_time : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Albums").AddColumn("LastSearchTime").AsDateTimeOffset().Nullable();
        }
    }
}
