using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class ReleaseMediumCountSpecificationFixture : CoreTest<ReleaseMediumCountSpecification>
    {
        private RemoteAlbum _remoteAlbum;
        private Album _album;

        [SetUp]
        public void Setup()
        {
            _album = Builder<Album>.CreateNew()
                .With(x => x.PreferredMediumCounts = new List<int>())
                .With(x => x.PreferredTrackCounts = new List<int>())
                .Build();

            _remoteAlbum = new RemoteAlbum
            {
                Albums = new List<Album> { _album },
                Release = new ReleaseInfo { Title = "Artist - Album (2CD Deluxe Edition) (2010) [FLAC]" }
            };
        }

        [TestCase("Artist - Album (2CD Deluxe Edition) (2010) [FLAC]", 2)]
        [TestCase("Artist - Album 2 CD", 2)]
        [TestCase("Artist - Album [3xCD]", 3)]
        [TestCase("Artist - Album (4 Disc Box Set)", 4)]
        [TestCase("Artist - Album 2-CD", 2)]
        [TestCase("Artist - Album 2x CD", 2)]
        public void should_parse_disc_count(string title, int expected)
        {
            ReleaseMediumCountSpecification.ParseDiscCount(title).Should().Be(expected);
        }

        [TestCase("Artist - Album (2010) [FLAC]")]
        [TestCase("Artist - Album 2010 CD FLAC")]
        [TestCase("Artist - Album (CD1)")]
        [TestCase("Artist - Album 1CD")]
        [TestCase("Artist - Album 16-bit CD")]
        [TestCase("Artist - Best Of Vol.2021CD")]
        [TestCase("")]
        [TestCase(null)]
        public void should_not_parse_disc_count(string title)
        {
            ReleaseMediumCountSpecification.ParseDiscCount(title).Should().BeNull();
        }

        [Test]
        public void should_accept_when_album_has_no_medium_filter()
        {
            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_title_has_no_disc_count()
        {
            _album.PreferredMediumCounts = new List<int> { 1 };
            _remoteAlbum.Release.Title = "Artist - Album (2010) [FLAC]";

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_disc_count_is_allowed()
        {
            _album.PreferredMediumCounts = new List<int> { 1, 2 };

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_disc_count_is_excluded()
        {
            _album.PreferredMediumCounts = new List<int> { 1 };

            var decision = Subject.IsSatisfiedBy(_remoteAlbum, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Contain("2 disc");
        }

        [Test]
        public void should_accept_when_albums_are_null()
        {
            _remoteAlbum.Albums = null;

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }
    }
}
