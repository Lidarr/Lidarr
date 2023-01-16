using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(081)]
    public class update_images_remote_url : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"Albums\" SET \"Images\" = REPLACE(\"Images\", '\"url\"', '\"remoteUrl\"')");
            Execute.Sql("UPDATE \"ArtistMetadata\" SET \"Images\" = REPLACE(\"Images\", '\"url\"', '\"remoteUrl\"')");
        }
    }
}
