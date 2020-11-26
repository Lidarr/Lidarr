using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class ProtocolSpecificationFixture : CoreTest<ProtocolSpecification>
    {
        private RemoteAlbum _remoteAlbum;
        private DelayProfile _delayProfile;

        [SetUp]
        public void Setup()
        {
            _remoteAlbum = new RemoteAlbum();
            _remoteAlbum.Release = new ReleaseInfo();
            _remoteAlbum.Artist = new Artist();

            _delayProfile = new DelayProfile();
            _delayProfile.Items.ForEach(x => x.Allowed = false);

            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.BestForTags(It.IsAny<HashSet<int>>()))
                  .Returns(_delayProfile);
        }

        private void GivenProtocol(DownloadProtocol downloadProtocol)
        {
            _remoteAlbum.Release.DownloadProtocol = downloadProtocol;
        }

        [Test]
        public void should_be_true_if_usenet_and_usenet_is_enabled()
        {
            GivenProtocol(DownloadProtocol.Usenet);
            _delayProfile.Items.Single(x => x.Protocol == DownloadProtocol.Usenet).Allowed = true;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(true);
        }

        [Test]
        public void should_be_true_if_torrent_and_torrent_is_enabled()
        {
            GivenProtocol(DownloadProtocol.Torrent);
            _delayProfile.Items.Single(x => x.Protocol == DownloadProtocol.Torrent).Allowed = true;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(true);
        }

        [Test]
        public void should_be_false_if_usenet_and_usenet_is_disabled()
        {
            GivenProtocol(DownloadProtocol.Usenet);

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(false);
        }

        [Test]
        public void should_be_false_if_torrent_and_torrent_is_disabled()
        {
            GivenProtocol(DownloadProtocol.Torrent);

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(false);
        }
    }
}
