using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(14)]
    public class fix_language_metadata_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"Artists\" SET \"MetadataProfileId\" = " +
                        "CASE WHEN ((SELECT COUNT(*) FROM \"MetadataProfiles\") > 0) " +
                        "THEN (SELECT \"Id\" FROM \"MetadataProfiles\" ORDER BY \"Id\" ASC LIMIT 1) " +
                        "ELSE 0 END " +
                        "WHERE \"Artists\".\"MetadataProfileId\" = 0");

            Execute.Sql("UPDATE \"Artists\" SET \"LanguageProfileId\" = " +
                        "CASE WHEN ((SELECT COUNT(*) FROM \"LanguageProfiles\") > 0) " +
                        "THEN (SELECT \"Id\" FROM \"LanguageProfiles\" ORDER BY \"Id\" ASC LIMIT 1) " +
                        "ELSE 0 END " +
                        "WHERE \"Artists\".\"LanguageProfileId\" = 0");
        }
    }
}
