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

        private void GivenProtocol(string downloadProtocol)
        {
            _remoteAlbum.Release.DownloadProtocol = downloadProtocol;
        }

        [Test]
        public void should_be_true_if_usenet_and_usenet_is_enabled()
        {
            GivenProtocol(nameof(UsenetDownloadProtocol));
            _delayProfile.Items.Single(x => x.Protocol == nameof(UsenetDownloadProtocol)).Allowed = true;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(true);
        }

        [Test]
        public void should_be_true_if_torrent_and_torrent_is_enabled()
        {
            GivenProtocol(nameof(TorrentDownloadProtocol));
            _delayProfile.Items.Single(x => x.Protocol == nameof(TorrentDownloadProtocol)).Allowed = true;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(true);
        }

        [Test]
        public void should_be_false_if_usenet_and_usenet_is_disabled()
        {
            GivenProtocol(nameof(UsenetDownloadProtocol));

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(false);
        }

        [Test]
        public void should_be_false_if_torrent_and_torrent_is_disabled()
        {
            GivenProtocol(nameof(TorrentDownloadProtocol));

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(false);
        }
    }
}
