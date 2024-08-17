using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(079)]
    public class add_indexes_album_statistics : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index().OnTable("Albums").OnColumn("Monitored");
            Create.Index().OnTable("Albums").OnColumn("ReleaseDate");
            Create.Index().OnTable("AlbumReleases").OnColumn("Monitored");
        }
    }
}
