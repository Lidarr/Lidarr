using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(082)]
    public class add_artist_preferred_name : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Artists").AddColumn("PreferredName").AsString().Nullable();
            Alter.Table("ArtistMetadata").AddColumn("SortName").AsString().Nullable();
        }
    }
}
