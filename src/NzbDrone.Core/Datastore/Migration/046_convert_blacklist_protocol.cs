using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(046)]
    public class convert_blacklist_protocol : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Blacklist").AlterColumn("Protocol").AsString().Nullable();

            Execute.Sql(@"DELETE FROM Blacklist WHERE Protocol=0");
            Execute.Sql(@"UPDATE Blacklist SET Protocol='UsenetDownloadProtocol' WHERE Protocol=1");
            Execute.Sql(@"UPDATE Blacklist SET Protocol='TorrentDownloadProtocol' WHERE Protocol=2");
        }
    }
}
