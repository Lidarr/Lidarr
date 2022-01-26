using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedReleasesFixture : DbTest<CleanupOrphanedReleases, AlbumRelease>
    {
        [Test]
        public void should_delete_orphaned_releases()
        {
            var albumRelease = Builder<AlbumRelease>.CreateNew()
                .BuildNew();

            Db.Insert(albumRelease);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_albums()
        {
            var album = Builder<Album>.CreateNew()
                .BuildNew();

            Db.Insert(album);

            var albumReleases = Builder<AlbumRelease>.CreateListOfSize(2)
                .TheFirst(1)
                .With(e => e.AlbumId = album.Id)
                .BuildListOfNew();

            Db.InsertMany(albumReleases);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.Should().Contain(e => e.AlbumId == album.Id);
        }
    }
}
