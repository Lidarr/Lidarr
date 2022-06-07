using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(055)]
    public class download_client_per_indexer : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Indexers").AddColumn("DownloadClientId").AsInt32().WithDefaultValue(0);
        }
    }
}
