using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(073)]
    public class add_flac_cue : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("TrackFiles").AddColumn("IsSingleFileRelease").AsBoolean().WithDefaultValue(false);
        }
    }
}
