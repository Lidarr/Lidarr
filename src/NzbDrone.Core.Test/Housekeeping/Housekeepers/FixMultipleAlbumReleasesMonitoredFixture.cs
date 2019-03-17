using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using System.Linq;
using NzbDrone.Test.Common;
using Moq;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class FixMultipleAlbumReleasesMonitoredFixture : DbTest<FixMultipleAlbumReleasesMonitored, AlbumRelease>
    {
        private ReleaseRepository _releaseRepo;
        
        [SetUp]
        public void Setup()
        {
            _releaseRepo = Mocker.Resolve<ReleaseRepository>();

            // do it like this so we can verify no call to SetMonitored
            // in the case where one release is monitored
            Mocker.GetMock<IReleaseRepository>()
                .Setup(x => x.FindByAlbum(It.IsAny<int>()))
                .Returns((int id) => _releaseRepo.FindByAlbum(id));
            
            Mocker.GetMock<IReleaseRepository>()
                .Setup(x => x.SetMonitored(It.IsAny<AlbumRelease>()))
                .Returns((AlbumRelease a) => _releaseRepo.SetMonitored(a));
        }
        
        [Test]
        public void should_unmonitor_some_if_too_many_monitored()
        {
            var releases = Builder<AlbumRelease>
                .CreateListOfSize(10)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Monitored = false)
                .With(x => x.AlbumId = 1)
                .Random(3)
                .With(x => x.Monitored = true)
                .BuildList();
            
            _releaseRepo.InsertMany(releases);
            
            _releaseRepo.All().Count(x => x.Monitored).Should().Be(3);
        
            Subject.Clean();
            
            _releaseRepo.All().Count(x => x.Monitored).Should().Be(1);
            
            // Count sentry and standard
            ExceptionVerification.ExpectedWarns(2);
        }
        
        [Test]
        public void should_monitor_one_if_none_monitored()
        {
            var releases = Builder<AlbumRelease>
                .CreateListOfSize(10)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Monitored = false)
                .With(x => x.AlbumId = 1)
                .BuildList();
            
            _releaseRepo.InsertMany(releases);
            
            _releaseRepo.All().Count(x => x.Monitored).Should().Be(0);
        
            Subject.Clean();
            
            _releaseRepo.All().Count(x => x.Monitored).Should().Be(1);

            // Count sentry and standard
            ExceptionVerification.ExpectedWarns(2);
        }

        [Test]
        public void no_change_if_one_monitored()
        {
            var releases = Builder<AlbumRelease>
                .CreateListOfSize(10)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Monitored = false)
                .With(x => x.AlbumId = 1)
                .Random(1)
                .With(x => x.Monitored = true)
                .BuildList();

            _releaseRepo.InsertMany(releases);

            _releaseRepo.All().Count(x => x.Monitored).Should().Be(1);

            Subject.Clean();

            _releaseRepo.All().Count(x => x.Monitored).Should().Be(1);
            
            Mocker.GetMock<IReleaseRepository>().Verify(x => x.SetMonitored(It.IsAny<AlbumRelease>()), Times.Never());
        }
    }
}
