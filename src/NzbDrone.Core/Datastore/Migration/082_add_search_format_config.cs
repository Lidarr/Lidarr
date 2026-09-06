using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(082)]
    public class add_search_format_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("SearchFormatConfig")
                  .WithColumn("UseCustomSearchFormat").AsBoolean().NotNullable().WithDefaultValue(false)
                  .WithColumn("AlbumSearchFormat").AsString().Nullable()
                  .WithColumn("ArtistSearchFormat").AsString().Nullable();
        }
    }
}
