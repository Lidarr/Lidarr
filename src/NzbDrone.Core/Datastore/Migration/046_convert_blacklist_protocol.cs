using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(046)]
    public class convert_blacklist_protocol : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // This may get run after the blacklist -> blocklist migration
            var table = Schema.Table("Blacklist").Exists() ? "Blacklist" : "Blocklist";

            Alter.Table(table).AlterColumn("Protocol").AsString().Nullable();

            Execute.Sql($"DELETE FROM \"{table}\" WHERE \"Protocol\" = '0'");
            Execute.Sql($"UPDATE \"{table}\" SET \"Protocol\" = 'UsenetDownloadProtocol' WHERE \"Protocol\" = '1'");
            Execute.Sql($"UPDATE \"{table}\" SET \"Protocol\" = 'TorrentDownloadProtocol' WHERE \"Protocol\" = '2'");
        }
    }
}
