using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class add_album_auto_blocklist_unwanted_releaseFixture : MigrationTest<add_album_auto_blocklist_unwanted_release>
    {
        [Test]
        public void should_default_flag_to_false_for_existing_albums()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Albums").Row(new
                {
                    Id = 1,
                    ForeignAlbumId = "1",
                    ArtistMetadataId = 1,
                    Title = "Album",
                    CleanTitle = "album",
                    Images = "",
                    Monitored = true,
                    AlbumType = "Studio",
                    AnyReleaseOk = true,
                    OldForeignAlbumIds = "[]"
                });
            });

            var albums = db.Query<AlbumAutoBlocklist083>("SELECT \"Id\", \"AutoBlocklistUnwantedRelease\" FROM \"Albums\"").ToList();

            albums.Should().HaveCount(1);
            albums.First().AutoBlocklistUnwantedRelease.Should().BeFalse();
        }
    }

    public class AlbumAutoBlocklist083
    {
        public int Id { get; set; }
        public bool AutoBlocklistUnwantedRelease { get; set; }
    }
}
