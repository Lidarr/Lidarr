using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(080)]
    public class update_redacted_baseurl : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"Indexers\" SET \"Settings\" = Replace(\"Settings\", '//redacted.ch', '//redacted.sh') WHERE \"Implementation\" = 'Redacted'");
        }
    }
}
