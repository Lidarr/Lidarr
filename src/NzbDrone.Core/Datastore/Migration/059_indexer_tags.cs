using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(059)]
    public class add_indexer_tags : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.FromTable("Indexers").Row(new { Implementation = "Omgwtfnzbs" });
            Delete.FromTable("Indexers").Row(new { Implementation = "Waffles" });

            Alter.Table("Indexers").AddColumn("Tags").AsString().Nullable();
        }
    }
}
