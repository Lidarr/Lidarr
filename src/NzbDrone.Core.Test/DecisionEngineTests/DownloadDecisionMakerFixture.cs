using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Test.Common;
using FizzWare.NBuilder;
using Marr.Data;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class DownloadDecisionMakerFixture : CoreTest<DownloadDecisionMaker>
    {
        private List<ReleaseInfo> _reports;
        private RemoteAlbum _remoteAlbum;

        private Mock<IDecisionEngineSpecification> _pass1;
        private Mock<IDecisionEngineSpecification> _pass2;
        private Mock<IDecisionEngineSpecification> _pass3;

        private Mock<IDecisionEngineSpecification> _fail1;
        private Mock<IDecisionEngineSpecification> _fail2;
        private Mock<IDecisionEngineSpecification> _fail3;

        private Mock<IDecisionEngineSpecification> _failDelayed1;

        [SetUp]
        public void Setup()
        {
            _pass1 = new Mock<IDecisionEngineSpecification>();
            _pass2 = new Mock<IDecisionEngineSpecification>();
            _pass3 = new Mock<IDecisionEngineSpecification>();

            _fail1 = new Mock<IDecisionEngineSpecification>();
            _fail2 = new Mock<IDecisionEngineSpecification>();
            _fail3 = new Mock<IDecisionEngineSpecification>();

            _failDelayed1 = new Mock<IDecisionEngineSpecification>();

            _pass1.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null)).Returns(Decision.Accept);
            _pass2.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null)).Returns(Decision.Accept);
            _pass3.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null)).Returns(Decision.Accept);
            
            _fail1.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null)).Returns(Decision.Reject("fail1"));
            _fail2.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null)).Returns(Decision.Reject("fail2"));
            _fail3.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null)).Returns(Decision.Reject("fail3"));

            _failDelayed1.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null)).Returns(Decision.Reject("failDelayed1"));
            _failDelayed1.SetupGet(c => c.Priority).Returns(SpecificationPriority.Disk);

            _reports = new List<ReleaseInfo> { new ReleaseInfo { Title = "Coldplay-A Head Full Of Dreams-CD-FLAC-2015-PERFECT" } };
            _remoteAlbum = new RemoteAlbum {
                Artist = new Artist(),
                Albums = new List<Album> { new Album() }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(c => c.Map(It.IsAny<ParsedAlbumInfo>(), It.IsAny<SearchCriteriaBase>()))
                  .Returns(_remoteAlbum);
        }

        private void GivenSpecifications(params Mock<IDecisionEngineSpecification>[] mocks)
        {
            Mocker.SetConstant<IEnumerable<IDecisionEngineSpecification>>(mocks.Select(c => c.Object));
        }

        [Test]
        public void should_call_all_specifications()
        {
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            Subject.GetRssDecision(_reports).ToList();

            _fail1.Verify(c => c.IsSatisfiedBy(_remoteAlbum, null), Times.Once());
            _fail2.Verify(c => c.IsSatisfiedBy(_remoteAlbum, null), Times.Once());
            _fail3.Verify(c => c.IsSatisfiedBy(_remoteAlbum, null), Times.Once());
            _pass1.Verify(c => c.IsSatisfiedBy(_remoteAlbum, null), Times.Once());
            _pass2.Verify(c => c.IsSatisfiedBy(_remoteAlbum, null), Times.Once());
            _pass3.Verify(c => c.IsSatisfiedBy(_remoteAlbum, null), Times.Once());
        }

        [Test]
        public void should_call_delayed_specifications_if_non_delayed_passed()
        {
            GivenSpecifications(_pass1, _failDelayed1);

            Subject.GetRssDecision(_reports).ToList();
            _failDelayed1.Verify(c => c.IsSatisfiedBy(_remoteAlbum, null), Times.Once());
        }

        [Test]
        public void should_not_call_delayed_specifications_if_non_delayed_failed()
        {
            GivenSpecifications(_fail1, _failDelayed1);

            Subject.GetRssDecision(_reports).ToList();

            _failDelayed1.Verify(c => c.IsSatisfiedBy(_remoteAlbum, null), Times.Never());
        }

        [Test]
        public void should_return_rejected_if_single_specs_fail()
        {
            GivenSpecifications(_fail1);

            var result = Subject.GetRssDecision(_reports);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_rejected_if_one_of_specs_fail()
        {
            GivenSpecifications(_pass1, _fail1, _pass2, _pass3);

            var result = Subject.GetRssDecision(_reports);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_pass_if_all_specs_pass()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            var result = Subject.GetRssDecision(_reports);

            result.Single().Approved.Should().BeTrue();
        }

        [Test]
        public void should_have_same_number_of_rejections_as_specs_that_failed()
        {
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            var result = Subject.GetRssDecision(_reports);
            result.Single().Rejections.Should().HaveCount(3);
        }

        [Test]
        public void should_not_attempt_to_map_album_if_not_parsable()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);
            _reports[0].Title = "Not parsable";

            var results = Subject.GetRssDecision(_reports).ToList();

            Mocker.GetMock<IParsingService>().Verify(c => c.Map(It.IsAny<ParsedAlbumInfo>(), It.IsAny<SearchCriteriaBase>()), Times.Never());

            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());

            results.Should().BeEmpty();
        }

        [Test]
        public void should_not_attempt_to_map_album_artist_title_is_blank()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);
            _reports[0].Title = "2013 - Night Visions";

            var results = Subject.GetRssDecision(_reports).ToList();

            Mocker.GetMock<IParsingService>().Verify(c => c.Map(It.IsAny<ParsedAlbumInfo>(), It.IsAny<SearchCriteriaBase>()), Times.Never());

            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());

            results.Should().BeEmpty();
        }

        [Test]
        public void should_not_attempt_to_make_decision_if_artist_is_unknown()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            _remoteAlbum.Artist = null;

            Subject.GetRssDecision(_reports);

            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteAlbum>(), null), Times.Never());
        }

        [Test]
        public void broken_report_shouldnt_blowup_the_process()
        {
            GivenSpecifications(_pass1);

            Mocker.GetMock<IParsingService>().Setup(c => c.Map(It.IsAny<ParsedAlbumInfo>(), It.IsAny<SearchCriteriaBase>()))
                     .Throws<TestException>();

            _reports = new List<ReleaseInfo>
                {
                    new ReleaseInfo{Title = "Coldplay-A Head Full Of Dreams-CD-FLAC-2015-PERFECT"},
                    new ReleaseInfo{Title = "Coldplay-A Head Full Of Dreams-CD-FLAC-2015-PERFECT"},
                    new ReleaseInfo{Title = "Coldplay-A Head Full Of Dreams-CD-FLAC-2015-PERFECT"}
                };

            Subject.GetRssDecision(_reports);

            Mocker.GetMock<IParsingService>().Verify(c => c.Map(It.IsAny<ParsedAlbumInfo>(), It.IsAny<SearchCriteriaBase>()), Times.Exactly(_reports.Count));

            ExceptionVerification.ExpectedErrors(3);
        }

        [Test]
        public void should_return_unknown_artist_rejection_if_artist_is_unknown()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            _remoteAlbum.Artist = null;

            var result = Subject.GetRssDecision(_reports);

            result.Should().HaveCount(1);
        }

        [Test]
        public void should_only_include_reports_for_requested_albums()
        {
            var artist = Builder<Artist>.CreateNew().Build();

            var albums = Builder<Album>.CreateListOfSize(2)
                .All()
                .With(v => v.ArtistId, artist.Id)
                .With(v => v.Artist, new LazyLoaded<Artist>(artist))
                .BuildList();

            var criteria = new ArtistSearchCriteria { Albums = albums.Take(1).ToList()};

            var reports = albums.Select(v => 
                new ReleaseInfo() 
                { 
                    Title = string.Format("{0}-{1}[FLAC][2017][DRONE]", artist.Name, v.Title) 
                }).ToList();

            Mocker.GetMock<IParsingService>()
                .Setup(v => v.Map(It.IsAny<ParsedAlbumInfo>(), It.IsAny<SearchCriteriaBase>()))
                .Returns<ParsedAlbumInfo, SearchCriteriaBase>((p,c) =>
                    new RemoteAlbum
                    {
                        DownloadAllowed = true,
                        ParsedAlbumInfo = p,
                        Artist = artist,
                        Albums = albums.Where(v => v.Title == p.AlbumTitle).ToList()
                    });

            Mocker.SetConstant<IEnumerable<IDecisionEngineSpecification>>(new List<IDecisionEngineSpecification>
            {
                Mocker.Resolve<NzbDrone.Core.DecisionEngine.Specifications.Search.AlbumRequestedSpecification>()
            });

            var decisions = Subject.GetSearchDecision(reports, criteria);

            var approvedDecisions = decisions.Where(v => v.Approved).ToList();

            approvedDecisions.Count.Should().Be(1);
        }

        [Test]
        public void should_not_allow_download_if_artist_is_unknown()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            _remoteAlbum.Artist = null;

            var result = Subject.GetRssDecision(_reports);

            result.Should().HaveCount(1);

            result.First().RemoteAlbum.DownloadAllowed.Should().BeFalse();
        }

        [Test]
        public void should_not_allow_download_if_no_albums_found()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            _remoteAlbum.Albums = new List<Album>();

            var result = Subject.GetRssDecision(_reports);

            result.Should().HaveCount(1);

            result.First().RemoteAlbum.DownloadAllowed.Should().BeFalse();
        }

        [Test]
        public void should_return_a_decision_when_exception_is_caught()
        {
            GivenSpecifications(_pass1);

            Mocker.GetMock<IParsingService>().Setup(c => c.Map(It.IsAny<ParsedAlbumInfo>(), It.IsAny<SearchCriteriaBase>()))
                     .Throws<TestException>();

            _reports = new List<ReleaseInfo>
                {
                    new ReleaseInfo{Title = "Alien Ant Farm - TruAnt (FLAC) DRONE"},
                };

            Subject.GetRssDecision(_reports).Should().HaveCount(1);

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
