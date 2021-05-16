using System;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Redacted;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.GazelleTests
{
    [TestFixture]
    public class RedactedFixture : CoreTest<Redacted>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition()
            {
                Name = "Redacted",
                Settings = new RedactedSettings
                {
                    ApiKey = "key"
                }
            };
        }

        [Test]
        public void should_parse_recent_feed_from_redacted()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Gazelle/Gazelle.json");
            var indexFeed = ReadAllText(@"Files/Indexers/Gazelle/GazelleIndex.json");

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(v => v.Method == HttpMethod.GET &&
                                                         v.Url.FullUri.Contains("ajax.php?action=browse") &&
                                                         v.Headers.Get("Authorization") == ((RedactedSettings)Subject.Definition.Settings).ApiKey)))
                                                         .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader { ContentType = "application/json" }, recentFeed));

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(v => v.Method == HttpMethod.GET &&
                                                         v.Url.FullUri.Contains("ajax.php?action=index") &&
                                                         v.Headers.Get("Authorization") == ((RedactedSettings)Subject.Definition.Settings).ApiKey)))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), indexFeed));

            ((RedactedRequestGenerator)Subject.GetRequestGenerator()).Authenticate();
            var releases = Subject.FetchRecent();

            releases.Should().HaveCount(4);

            var releaseInfo = releases.First();

            releaseInfo.Title.Should().Be("Shania Twain - Shania Twain (1993) [FLAC 24bit Lossless] [WEB]");
            releaseInfo.DownloadProtocol.Should().Be(nameof(TorrentDownloadProtocol));
            releaseInfo.DownloadUrl.Should()
                .Be("https://redacted.ch/torrents.php?action=download&id=1541452&authkey=lidarr&torrent_pass=redacted&usetoken=0");
            releaseInfo.InfoUrl.Should().Be("https://redacted.ch/torrents.php?id=106951&torrentid=1541452");
            releaseInfo.CommentUrl.Should().Be(null);
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("2017-12-11 00:17:53"));
            releaseInfo.Size.Should().Be(653734702);
        }
    }
}
