using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    public class NewznabRequestGeneratorFixture : CoreTest<NewznabRequestGenerator>
    {
        private AlbumSearchCriteria _singleAlbumSearchCriteria;
        private NewznabCapabilities _capabilities;

        [SetUp]
        public void SetUp()
        {
            Subject.Settings = new NewznabSettings()
            {
                BaseUrl = "http://127.0.0.1:1234/",
                Categories = new[] { 1, 2 },
                ApiKey = "abcd",
            };

            _singleAlbumSearchCriteria = new AlbumSearchCriteria
            {
                Artist = new Music.Artist { Name = "Alien Ant Farm" },
                AlbumTitle = "TruANT"
            };

            _capabilities = new NewznabCapabilities();

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>()))
                .Returns(_capabilities);
        }

        [Test]
        public void should_use_all_categories_for_feed()
        {
            var results = Subject.GetRecentRequests();

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("&cat=1,2&");
        }

        [Test]
        public void should_search_by_artist_and_album_if_supported()
        {
            _capabilities.SupportedAudioSearchParameters = new[] { "q", "artist", "album" };

            var results = Subject.GetSearchRequests(_singleAlbumSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("artist=Alien%20Ant%20Farm");
            page.Url.Query.Should().Contain("album=TruANT");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void should_search_for_a_song_without_an_album_filter(bool supportsTrack)
        {
            _capabilities.SupportedAudioSearchParameters = supportsTrack ?
                new[] { "q", "artist", "track" } : new[] { "q", "artist", "album" };
            var criteria = new TrackSearchCriteria
            {
                Artist = _singleAlbumSearchCriteria.Artist,
                AlbumTitle = "These Days"
            };

            var requests = Subject.GetSearchRequests(criteria);
            var page = requests.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("t=music");
            page.Url.Query.Should().NotContain("&album=");
            page.Url.Query.Should().Contain(supportsTrack ? "track=These%20Days" : "q=Alien%20Ant%20Farm%20These%20Days");
        }
    }
}
