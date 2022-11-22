using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AlbumTests
{
    [TestFixture]
    public class MonitorNewAlbumServiceFixture : CoreTest<MonitorNewAlbumService>
    {
        private List<Album> _albums;

        [SetUp]
        public void Setup()
        {
            _albums = Builder<Album>.CreateListOfSize(4)
                .All()
                .With(e => e.Monitored = true)
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-7))

                // Future
                .TheFirst(1)
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(7))

                // Future/TBA
                .TheNext(1)
                .With(e => e.ReleaseDate = null)
                .Build()
                .ToList();
        }

        [Test]
        public void should_monitor_with_all()
        {
            foreach (var album in _albums)
            {
                Subject.ShouldMonitorNewAlbum(album, _albums, NewItemMonitorTypes.All).Should().BeTrue();
            }
        }

        [Test]
        public void should_not_monitor_with_none()
        {
            foreach (var album in _albums)
            {
                Subject.ShouldMonitorNewAlbum(album, _albums, NewItemMonitorTypes.None).Should().BeFalse();
            }
        }

        [Test]
        public void should_only_monitor_new_with_new()
        {
            Subject.ShouldMonitorNewAlbum(_albums[0], _albums, NewItemMonitorTypes.New).Should().BeTrue();

            foreach (var album in _albums.Skip(1))
            {
                Subject.ShouldMonitorNewAlbum(album, _albums, NewItemMonitorTypes.New).Should().BeFalse();
            }
        }
    }
}
