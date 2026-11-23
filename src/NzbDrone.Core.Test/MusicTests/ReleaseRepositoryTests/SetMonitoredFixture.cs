using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests.ReleaseRepositoryTests
{
    [TestFixture]
    public class SetMonitoredFixture : DbTest<ReleaseRepository, AlbumRelease>
    {
        private List<AlbumRelease> _releases;
        private AlbumRelease _otherAlbumRelease;

        [SetUp]
        public void Setup()
        {
            _releases = Builder<AlbumRelease>.CreateListOfSize(2)
                .All()
                .With(r => r.AlbumId = 1)
                .TheFirst(1)
                .With(r => r.ForeignReleaseId = "release-1")
                .With(r => r.ReleaseDate = new DateTime(1983, 1, 20, 0, 0, 0, DateTimeKind.Utc))
                .With(r => r.OldForeignReleaseIds = new List<string> { "old-release-1" })
                .With(r => r.Monitored = true)
                .TheNext(1)
                .With(r => r.ForeignReleaseId = "release-2")
                .With(r => r.ReleaseDate = new DateTime(1983, 3, 1, 0, 0, 0, DateTimeKind.Utc))
                .With(r => r.OldForeignReleaseIds = new List<string> { "old-release-2" })
                .With(r => r.Monitored = false)
                .BuildListOfNew();

            _otherAlbumRelease = Builder<AlbumRelease>.CreateNew()
                .With(r => r.AlbumId = 2)
                .With(r => r.ForeignReleaseId = "release-3")
                .With(r => r.Monitored = true)
                .BuildNew();

            Subject.InsertMany(_releases);
            Subject.Insert(_otherAlbumRelease);
        }

        [Test]
        public void should_keep_release_dates_and_old_release_ids_when_monitored_release_is_set()
        {
            Subject.SetMonitored(_releases[1]);

            var stored = Subject.FindByAlbum(1);

            stored.Single(r => r.Id == _releases[0].Id).ReleaseDate.Should().Be(new DateTime(1983, 1, 20, 0, 0, 0, DateTimeKind.Utc));
            stored.Single(r => r.Id == _releases[0].Id).OldForeignReleaseIds.Should().Equal("old-release-1");
            stored.Single(r => r.Id == _releases[1].Id).ReleaseDate.Should().Be(new DateTime(1983, 3, 1, 0, 0, 0, DateTimeKind.Utc));
            stored.Single(r => r.Id == _releases[1].Id).OldForeignReleaseIds.Should().Equal("old-release-2");
        }

        [Test]
        public void should_monitor_only_the_given_release()
        {
            Subject.SetMonitored(_releases[1]);

            Subject.FindByAlbum(1).Where(r => r.Monitored).Select(r => r.Id).Should().Equal(_releases[1].Id);
            Subject.Get(_otherAlbumRelease.Id).Monitored.Should().BeTrue();
        }

        [Test]
        public void should_throw_when_release_is_not_on_the_album()
        {
            var release = new AlbumRelease { Id = _otherAlbumRelease.Id, AlbumId = 1 };

            Assert.Throws<ArgumentException>(() => Subject.SetMonitored(release));

            Subject.FindByAlbum(1).Where(r => r.Monitored).Select(r => r.Id).Should().Equal(_releases[0].Id);
            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
