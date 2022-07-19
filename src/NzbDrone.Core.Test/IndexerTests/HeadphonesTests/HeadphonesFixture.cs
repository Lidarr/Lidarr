using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Headphones;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.HeadphonesTests
{
    [TestFixture]
    public class HeadphonesFixture : CoreTest<Headphones>
    {
        private HeadphonesCapabilities _caps;

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "Headphones VIP",
                Settings = new HeadphonesSettings
                {
                    Categories = new int[] { 3000 },
                    Username = "user",
                    Password = "pass"
                }
            };

            _caps = new HeadphonesCapabilities();
            Mocker.GetMock<IHeadphonesCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<HeadphonesSettings>()))
                .Returns(_caps);
        }

        [Test]
        public async Task should_parse_recent_feed_from_headphones()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Headphones/Headphones.xml");

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.ExecuteAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get)))
                .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), recentFeed)));

            var releases = await Subject.FetchRecent();

            releases.Should().HaveCount(16);

            releases.First().Should().BeOfType<ReleaseInfo>();
            var releaseInfo = releases.First();

            releaseInfo.Title.Should().Be("Lady Gaga Born This Way 2CD FLAC 2011 WRE");
            releaseInfo.DownloadProtocol.Should().Be(nameof(UsenetDownloadProtocol));
            releaseInfo.DownloadUrl.Should().Be("https://indexer.codeshy.com/api?t=g&guid=123456&apikey=123456789");
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("2013/06/02 08:58:54"));
            releaseInfo.Size.Should().Be(917347414);
        }

        [Test]
        public void should_use_pagesize_reported_by_caps()
        {
            _caps.MaxPageSize = 30;
            _caps.DefaultPageSize = 25;

            Subject.PageSize.Should().Be(25);
        }
    }
}
