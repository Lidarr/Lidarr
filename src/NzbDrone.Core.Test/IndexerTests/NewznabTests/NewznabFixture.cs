using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    [TestFixture]
    public class NewznabFixture : CoreTest<Newznab>
    {
        private NewznabCapabilities _caps;

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition()
            {
                Id = 5,
                Name = "Newznab",
                Settings = new NewznabSettings()
                {
                    BaseUrl = "http://indexer.local/",
                    Categories = new int[] { 1 }
                }
            };

            _caps = new NewznabCapabilities();
            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>()))
                .Returns(_caps);
        }

        [Test]
        public async Task should_parse_recent_feed_from_newznab_nzb_su()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Newznab/newznab_nzb_su.xml");

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.ExecuteAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get)))
                .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), recentFeed)));

            var releases = await Subject.FetchRecent();

            releases.Should().HaveCount(100);

            var releaseInfo = releases.First();

            releaseInfo.Title.Should().Be("Brainstorm-Scary Creatures-CD-FLAC-2016-NBFLAC");
            releaseInfo.DownloadProtocol.Should().Be(nameof(UsenetDownloadProtocol));
            releaseInfo.DownloadUrl.Should().Be("http://api.nzbgeek.info/api?t=get&id=38884827e1e56b9336278a449e0a38ec&apikey=xxx");
            releaseInfo.InfoUrl.Should().Be("https://nzbgeek.info/geekseek.php?guid=38884827e1e56b9336278a449e0a38ec");
            releaseInfo.CommentUrl.Should().Be("https://nzbgeek.info/geekseek.php?guid=38884827e1e56b9336278a449e0a38ec");
            releaseInfo.IndexerId.Should().Be(Subject.Definition.Id);
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("2017/05/26 05:54:31"));
            releaseInfo.Size.Should().Be(492735000);
        }

        [Test]
        public void should_use_best_pagesize_reported_by_caps()
        {
            _caps.MaxPageSize = 30;
            _caps.DefaultPageSize = 25;

            Subject.PageSize.Should().Be(30);
        }

        [Test]
        public void should_not_use_pagesize_over_100_even_if_reported_in_caps()
        {
            _caps.MaxPageSize = 250;
            _caps.DefaultPageSize = 25;

            Subject.PageSize.Should().Be(100);
        }

        [Test]
        public async Task should_record_indexer_failure_if_caps_throw()
        {
            var request = new HttpRequest("http://my.indexer.com");
            var response = new HttpResponse(request, new HttpHeader(), Array.Empty<byte>(), (HttpStatusCode)429);
            response.Headers["Retry-After"] = "300";

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>()))
                .Throws(new TooManyRequestsException(request, response));

            _caps.MaxPageSize = 30;
            _caps.DefaultPageSize = 25;

            var releases = await Subject.FetchRecent();

            releases.Should().BeEmpty();

            Mocker.GetMock<IIndexerStatusService>()
                  .Verify(v => v.RecordFailure(It.IsAny<int>(), TimeSpan.FromMinutes(5.0)), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public async Task should_parse_languages()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Newznab/newznab_language.xml");

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.ExecuteAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get)))
                .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), recentFeed)));

            var releases = await Subject.FetchRecent();

            releases.Should().HaveCount(100);

            releases[0].Languages.Should().BeEquivalentTo(new[] { Language.English, Language.Japanese });
            releases[1].Languages.Should().BeEquivalentTo(new[] { Language.English, Language.Spanish });
            releases[2].Languages.Should().BeEquivalentTo(new[] { Language.French });
        }
    }
}
