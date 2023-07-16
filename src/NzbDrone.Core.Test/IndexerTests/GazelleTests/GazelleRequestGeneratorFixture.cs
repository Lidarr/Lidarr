using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.GazelleTests
{
    public class GazelleRequestGeneratorFixture : CoreTest<GazelleRequestGenerator>
    {
        private AlbumSearchCriteria _singleAlbumSearchCriteria;
        private AlbumSearchCriteria _variousArtistSearchCriteria;

        [SetUp]
        public void SetUp()
        {
            Subject.Settings = new GazelleSettings()
            {
                BaseUrl = "http://127.0.0.1:1234/",
                Username = "someuser",
                Password = "somepass"
            };

            _singleAlbumSearchCriteria = new AlbumSearchCriteria
            {
                Artist = new Music.Artist { Name = "Alien Ant Farm" },
                AlbumTitle = "TruANT"
            };

            _variousArtistSearchCriteria = new AlbumSearchCriteria
            {
                Artist = new Music.Artist { Name = "Various Artists" },
                AlbumTitle = "TruANT"
            };

            Mocker.GetMock<IHttpClient>()
                  .Setup(v => v.ExecuteAsync(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), "{ \"status\": \"success\", \"response\": { \"authkey\": \"key\", \"passkey\": \"key\" }  }")));

            Mocker.GetMock<ICached<Dictionary<string, string>>>()
                  .Setup(v => v.Find(It.IsAny<string>()))
                  .Returns<string>(r => new Dictionary<string, string> { { "some", "cookie" } });

            Subject.AuthCookieCache = Mocker.Resolve<ICached<Dictionary<string, string>>>();

            Subject.HttpClient = Mocker.Resolve<IHttpClient>();

            Subject.Logger = Mocker.Resolve<Logger>();
        }

        [Test]
        public void should_use_all_categories_for_feed()
        {
            var results = Subject.GetRecentRequests();

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Be("action=browse&searchstr=");
        }

        [Test]
        public void should_search_by_artist_and_album_if_supported()
        {
            var results = Subject.GetSearchRequests(_singleAlbumSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("artistname=Alien+Ant+Farm");
            page.Url.Query.Should().Contain("groupname=TruANT");
        }

        [Test]
        public void should_only_search_by_album_if_various_artist()
        {
            var results = Subject.GetSearchRequests(_variousArtistSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().NotContain("artistname=");
            page.Url.Query.Should().Contain("groupname=TruANT");
        }
    }
}
