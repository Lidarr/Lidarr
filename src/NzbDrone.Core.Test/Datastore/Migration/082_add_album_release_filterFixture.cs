using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class add_album_release_filterFixture : MigrationTest<add_album_release_filter>
    {
        [Test]
        public void should_add_empty_filter_columns_to_existing_albums()
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

            var albums = db.Query<AlbumFilter082>("SELECT \"Id\", \"PreferredTrackCounts\", \"PreferredMediumCounts\" FROM \"Albums\"").ToList();

            albums.Should().HaveCount(1);
            albums.First().PreferredTrackCounts.Should().Be("[]");
            albums.First().PreferredMediumCounts.Should().Be("[]");
        }
    }

    public class AlbumFilter082
    {
        public int Id { get; set; }
        public string PreferredTrackCounts { get; set; }
        public string PreferredMediumCounts { get; set; }
    }
}
