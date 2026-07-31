using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(083)]
    public class download_client_write_audio_tags : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("DownloadClients").AddColumn("WriteAudioTags").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
