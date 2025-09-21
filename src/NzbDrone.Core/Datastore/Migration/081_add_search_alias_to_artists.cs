using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(081)]
    public class add_search_alias_to_artists : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Artists")
                 .AddColumn("SearchAlias").AsString().Nullable();
        }
    }
}
