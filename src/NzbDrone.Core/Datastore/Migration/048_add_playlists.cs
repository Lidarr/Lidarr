using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(48)]
    public class add_playlists : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("Playlists")
                .WithColumn("ForeignPlaylistId").AsString().Unique()
                .WithColumn("Title").AsString()
                .WithColumn("OutputFolder").AsString().Nullable();

            Create.TableForModel("PlaylistEntries")
                .WithColumn("PlaylistId").AsInt32().Indexed()
                .WithColumn("Order").AsInt32()
                .WithColumn("ForeignAlbumId").AsString().Indexed()
                .WithColumn("TrackTitle").AsString();
        }
    }
}
