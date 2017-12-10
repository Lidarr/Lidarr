using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Test.MusicTests.AlbumRepositoryTests
{
    [TestFixture]

    public class AlbumRepositoryFixture : DbTest<AlbumRepository, Album>
    {
        private Artist _fakeArtist;

        [SetUp]
        public void Setup()
        {
            _fakeArtist = Builder<Artist>
                .CreateNew()
                .With(s => s.Path = @"C:\Test\Music\Artist\")
                .Build();
            
        }
        
        [Test]
        public void should_find_album_by_path()
        {
            var filename = @"C:\Test\Music\Artist\Album[Year]";

            var fakeAlbum = Builder<Album>.CreateNew().BuildNew();
            fakeAlbum.RelativePath = @"Album[Year]";
            fakeAlbum.ArtistId = _fakeArtist.Id;
            Subject.Insert(fakeAlbum);

            var album = Subject.FindByPath(filename, _fakeArtist);
            album.Should().NotBe(null);
        }
    }
}
