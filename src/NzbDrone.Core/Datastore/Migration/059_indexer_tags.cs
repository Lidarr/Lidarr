using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(059)]
    public class add_indexer_tags : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("DELETE FROM Indexers WHERE Implementation = 'Omgwtfnzbs'");
            Execute.Sql("DELETE FROM Indexers WHERE Implementation = 'Waffles'");

            Alter.Table("Indexers").AddColumn("Tags").AsString().Nullable();
        }
    }
}
