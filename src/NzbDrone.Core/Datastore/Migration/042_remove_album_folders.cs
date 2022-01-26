using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(42)]
    public class remove_album_folders : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("AlbumFolder").FromTable("Artists");

            Execute.Sql("UPDATE \"NamingConfig\" SET \"StandardTrackFormat\" = \"AlbumFolderFormat\" || '/' || \"StandardTrackFormat\"");
            Execute.Sql("UPDATE \"NamingConfig\" SET \"MultiDiscTrackFormat\" = \"AlbumFolderFormat\" || '/' || \"MultiDiscTrackFormat\"");

            Delete.Column("AlbumFolderFormat").FromTable("NamingConfig");
        }
    }
}
