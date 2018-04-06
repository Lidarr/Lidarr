using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NzbDrone.Core.Test.MusicTests.AlbumRepositoryTests
{
    [TestFixture]
    public class AlbumRepositoryFixture : DbTest<AlbumService, Album>
    {
        [Test]
        public void should_find_album_in_db_by_releaseid()
        {
            var id = "e00e40a3-5ed5-4ed3-9c22-0a8ff4119bdf";
            var artist = new Artist();
            artist.Name = "Alien Ant Farm";
            artist.Monitored = true;
            artist.MBId = "this is a fake id";
            artist.Id = 1;
            var album = new Album();
            album.Title = "ANThology";
            AlbumRelease release = new AlbumRelease();
            release.Id = id;
            album.ForeignAlbumId = "1";
            album.CleanTitle = "anthology";
            album.Artist = artist;
            album.AlbumType = "";

            album.Releases.Add(release);

            var albumRepo = Mocker.Resolve<AlbumRepository>();

            albumRepo.Insert(album);

            var builtAlbum = Builder<Album>.CreateNew().BuildNew();
            album.Title.Should().Be(albumRepo.FindAlbumByRelease(id).Title);
        }
    }
}
