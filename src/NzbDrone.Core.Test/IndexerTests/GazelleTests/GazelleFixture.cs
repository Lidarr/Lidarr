using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.GazelleTests
{
    [TestFixture]
    public class GazelleFixture : CoreTest<Gazelle>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition()
            {
                Name = "Gazelle",
                Settings = new GazelleSettings
                {
                    Username = "user",
                    Password = "pass",
                    BaseUrl = "http://someurl.ch"
                }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_gazelle()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Gazelle/Gazelle.json");
            var indexFeed = ReadAllText(@"Files/Indexers/Gazelle/GazelleIndex.json");

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.ExecuteAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.FullUri.Contains("ajax.php?action=browse"))))
                .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader { ContentType = "application/json" }, recentFeed)));

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.ExecuteAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Post && v.Url.FullUri.Contains("ajax.php?action=index"))))
                .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), indexFeed)));

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.ExecuteAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Post && v.Url.FullUri.Contains("login.php"))))
                .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), indexFeed)));

            var releases = await Subject.FetchRecent();

            releases.Should().HaveCount(4);

            var releaseInfo = releases.First();

            releaseInfo.Title.Should().Be("Shania Twain - Shania Twain (1993) [FLAC 24bit Lossless] [WEB]");
            releaseInfo.DownloadProtocol.Should().Be(nameof(TorrentDownloadProtocol));
            releaseInfo.DownloadUrl.Should()
                .Be("http://someurl.ch/torrents.php?action=download&id=1541452&authkey=redacted&torrent_pass=redacted");
            releaseInfo.InfoUrl.Should().Be("http://someurl.ch/torrents.php?id=106951&torrentid=1541452");
            releaseInfo.CommentUrl.Should().Be(null);
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("2017-12-11 00:17:53"));
            releaseInfo.Size.Should().Be(653734702);
        }
    }
}
