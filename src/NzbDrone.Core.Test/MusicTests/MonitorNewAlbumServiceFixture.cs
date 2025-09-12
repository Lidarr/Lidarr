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
                .With(e => e.Title = "Test Album")

                // Future
                .TheFirst(1)
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(7))
                .With(e => e.Title = "Future Album")

                // Future/TBA
                .TheNext(1)
                .With(e => e.ReleaseDate = null)
                .With(e => e.Title = "TBA Album")
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

        [Test]
        public void should_not_monitor_album_with_null_release_date()
        {
            var albumWithNullDate = Builder<Album>.CreateNew()
                .With(e => e.ReleaseDate = null)
                .With(e => e.Title = "No Date Album")
                .Build();

            var existingAlbums = Builder<Album>.CreateListOfSize(2)
                .All()
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-30))
                .Build()
                .ToList();

            Subject.ShouldMonitorNewAlbum(albumWithNullDate, existingAlbums, NewItemMonitorTypes.New)
                .Should().BeFalse();
        }

        [Test]
        public void should_monitor_album_when_no_existing_albums_have_dates()
        {
            var newAlbumWithDate = Builder<Album>.CreateNew()
                .With(e => e.ReleaseDate = DateTime.UtcNow)
                .With(e => e.Title = "New Album With Date")
                .Build();

            var existingAlbumsWithoutDates = Builder<Album>.CreateListOfSize(3)
                .All()
                .With(e => e.ReleaseDate = null)
                .Build()
                .ToList();

            Subject.ShouldMonitorNewAlbum(newAlbumWithDate, existingAlbumsWithoutDates, NewItemMonitorTypes.New)
                .Should().BeTrue();
        }

        [Test]
        public void should_monitor_album_newer_than_existing_albums()
        {
            var newerAlbum = Builder<Album>.CreateNew()
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(1))
                .With(e => e.Title = "Newer Album")
                .Build();

            var existingAlbums = Builder<Album>.CreateListOfSize(3)
                .All()
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-30))
                .TheFirst(1)
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-1)) // Most recent existing
                .Build()
                .ToList();

            Subject.ShouldMonitorNewAlbum(newerAlbum, existingAlbums, NewItemMonitorTypes.New)
                .Should().BeTrue();
        }

        [Test]
        public void should_not_monitor_album_older_than_existing_albums()
        {
            var olderAlbum = Builder<Album>.CreateNew()
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-10))
                .With(e => e.Title = "Older Album")
                .Build();

            var existingAlbums = Builder<Album>.CreateListOfSize(3)
                .All()
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-30))
                .TheFirst(1)
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-1)) // Most recent existing
                .Build()
                .ToList();

            Subject.ShouldMonitorNewAlbum(olderAlbum, existingAlbums, NewItemMonitorTypes.New)
                .Should().BeFalse();
        }

        [Test]
        public void should_monitor_album_with_same_date_as_existing_album()
        {
            var sameDate = DateTime.UtcNow.AddDays(-5);
            var albumWithSameDate = Builder<Album>.CreateNew()
                .With(e => e.ReleaseDate = sameDate)
                .With(e => e.Title = "Same Date Album")
                .Build();

            var existingAlbums = Builder<Album>.CreateListOfSize(3)
                .All()
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-30))
                .TheFirst(1)
                .With(e => e.ReleaseDate = sameDate) // Same date as new album
                .Build()
                .ToList();

            Subject.ShouldMonitorNewAlbum(albumWithSameDate, existingAlbums, NewItemMonitorTypes.New)
                .Should().BeTrue();
        }

        [Test]
        public void should_ignore_existing_albums_with_null_dates_when_finding_newest()
        {
            var newAlbum = Builder<Album>.CreateNew()
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(1))
                .With(e => e.Title = "New Album")
                .Build();

            var existingAlbums = Builder<Album>.CreateListOfSize(4)
                .All()
                .With(e => e.ReleaseDate = null) // All null dates
                .TheFirst(1)
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-5)) // Only one with actual date
                .Build()
                .ToList();

            Subject.ShouldMonitorNewAlbum(newAlbum, existingAlbums, NewItemMonitorTypes.New)
                .Should().BeTrue();
        }

        [Test]
        public void should_throw_for_unknown_monitor_type()
        {
            var album = _albums.First();
            Assert.Throws<NotImplementedException>(() =>
                Subject.ShouldMonitorNewAlbum(album, _albums, (NewItemMonitorTypes)999));
        }

        [Test]
        public void should_monitor_album_with_null_date_when_all_existing_albums_also_have_null_dates()
        {
            var albumWithNullDate = Builder<Album>.CreateNew()
                .With(e => e.ReleaseDate = null)
                .With(e => e.Title = "No Date Album")
                .Build();

            var existingAlbumsWithoutDates = Builder<Album>.CreateListOfSize(3)
                .All()
                .With(e => e.ReleaseDate = null)
                .Build()
                .ToList();

            Subject.ShouldMonitorNewAlbum(albumWithNullDate, existingAlbumsWithoutDates, NewItemMonitorTypes.New)
                .Should().BeTrue();
        }
    }
}
