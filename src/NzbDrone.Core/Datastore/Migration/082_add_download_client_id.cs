using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(082)]
    public class add_download_client_id : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("TrackFiles").AddColumn("DownloadClientId").AsInt32().WithDefaultValue(0);
        }
    }
}
