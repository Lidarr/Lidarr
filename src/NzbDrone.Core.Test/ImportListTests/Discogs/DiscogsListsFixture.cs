using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists.Discogs;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.Discogs;

[TestFixture]
public class DiscogsListsFixture : CoreTest<DiscogsLists>
{
    [SetUp]
    public void Setup()
    {
        Subject.Definition = new ImportLists.ImportListDefinition
        {
            Id = 1,
            Name = "Test Discogs List",
            Settings = new DiscogsListsSettings
            {
                Token = "test_token_123456",
                ListId = "123456",
                BaseUrl = "https://api.discogs.com"
            }
        };
    }

    [Test]
    public void should_parse_valid_list_response()
    {
        var listResponseJson = @"{
            ""items"": [
                {
                    ""type"": ""release"",
                    ""id"": 3,
                    ""display_title"": ""Josh Wink - Profound Sounds Vol. 1"",
                    ""resource_url"": ""https://api.discogs.com/releases/3""
                }
            ]
        }";

        var releaseResponseJson = @"{
            ""title"": ""Profound Sounds Vol. 1"",
            ""artists"": [
                {
                    ""name"": ""Josh Wink"",
                    ""id"": 3
                }
            ]
        }";

        var listCalled = false;
        var releaseCalled = false;
        var actualListUrl = "";
        var actualReleaseUrl = "";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
              .Callback<HttpRequest>(req =>
              {
                  if (req.Url.FullUri.Contains("/lists/"))
                  {
                      listCalled = true;
                      actualListUrl = req.Url.FullUri;
                  }
                  else if (req.Url.FullUri.Contains("/releases/"))
                  {
                      releaseCalled = true;
                      actualReleaseUrl = req.Url.FullUri;
                  }
              })
              .Returns<HttpRequest>(req =>
              {
                  if (req.Url.FullUri.Contains("/lists/"))
                  {
                      return new HttpResponse(req, new HttpHeader(), listResponseJson, HttpStatusCode.OK);
                  }
                  else if (req.Url.FullUri.Contains("/releases/"))
                  {
                      return new HttpResponse(req, new HttpHeader(), releaseResponseJson, HttpStatusCode.OK);
                  }
                  else
                  {
                      return new HttpResponse(req, new HttpHeader(), "", HttpStatusCode.NotFound);
                  }
              });

        var releases = Subject.Fetch();

        // Debug output to see what happened
        System.Console.WriteLine($"List called: {listCalled}, URL: {actualListUrl}");
        System.Console.WriteLine($"Release called: {releaseCalled}, URL: {actualReleaseUrl}");
        System.Console.WriteLine($"Releases count: {releases.Count}");
        for (var i = 0; i < releases.Count; i++)
        {
            System.Console.WriteLine($"Release {i}: Artist='{releases[i].Artist}', Album='{releases[i].Album}'");
        }

        releases.Should().HaveCount(1);
        releases.First().Artist.Should().Be("Josh Wink");
        releases.First().Album.Should().Be("Profound Sounds Vol. 1");
    }

    [Test]
    public void should_handle_empty_list_response()
    {
        var responseJson = @"{""items"": []}";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
              .Returns(new HttpResponse(new HttpRequest("https://api.discogs.com/lists/123456"), new HttpHeader(), responseJson, HttpStatusCode.OK));

        var releases = Subject.Fetch();

        releases.Should().BeEmpty();
    }

    [Test]
    public void debug_test_to_see_what_url_is_called()
    {
        var responseJson = @"{""items"": []}";
        var actualUrl = "";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
              .Callback<HttpRequest>(req => actualUrl = req.Url.FullUri)
              .Returns(new HttpResponse(new HttpRequest("https://api.discogs.com/lists/123456"), new HttpHeader(), responseJson, HttpStatusCode.OK));

        var releases = Subject.Fetch();

        // This will show us what URL is actually being called
        actualUrl.Should().NotBeEmpty();
    }

    [Test]
    public void should_skip_non_release_items()
    {
        var responseJson = @"{
            ""items"": [
                {
                    ""type"": ""label"",
                    ""id"": 1,
                    ""display_title"": ""Some Label"",
                    ""resource_url"": ""https://api.discogs.com/labels/1""
                },
                {
                    ""type"": ""release"",
                    ""id"": 3,
                    ""display_title"": ""Josh Wink - Profound Sounds Vol. 1"",
                    ""resource_url"": ""https://api.discogs.com/releases/3""
                }
            ]
        }";

        var releaseResponseJson = @"{
            ""title"": ""Profound Sounds Vol. 1"",
            ""artists"": [
                {
                    ""name"": ""Josh Wink"",
                    ""id"": 3
                }
            ]
        }";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/lists/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), responseJson, HttpStatusCode.OK));

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/releases/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), releaseResponseJson, HttpStatusCode.OK));

        var releases = Subject.Fetch();

        releases.Should().HaveCount(1);
        releases.First().Artist.Should().Be("Josh Wink");
    }

    [Test]
    public void should_skip_items_when_release_fetch_fails()
    {
        var listResponseJson = @"{
            ""items"": [
                {
                    ""type"": ""release"",
                    ""id"": 3,
                    ""display_title"": ""Josh Wink - Profound Sounds Vol. 1"",
                    ""resource_url"": ""https://api.discogs.com/releases/3""
                }
            ]
        }";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/lists/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), listResponseJson, HttpStatusCode.OK));

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/releases/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), "", HttpStatusCode.NotFound));

        var releases = Subject.Fetch();

        releases.Should().BeEmpty();
    }

    [Test]
    public void should_skip_releases_with_no_artists()
    {
        var listResponseJson = @"{
            ""items"": [
                {
                    ""type"": ""release"",
                    ""id"": 3,
                    ""display_title"": ""Various - Compilation"",
                    ""resource_url"": ""https://api.discogs.com/releases/3""
                }
            ]
        }";

        var releaseResponseJson = @"{
            ""title"": ""Compilation"",
            ""artists"": []
        }";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/lists/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), listResponseJson, HttpStatusCode.OK));

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/releases/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), releaseResponseJson, HttpStatusCode.OK));

        var releases = Subject.Fetch();

        releases.Should().BeEmpty();
    }

    [Test]
    public void should_use_first_artist_when_multiple_artists()
    {
        var listResponseJson = @"{
            ""items"": [
                {
                    ""type"": ""release"",
                    ""id"": 3,
                    ""display_title"": ""Artist 1 & Artist 2 - Collaboration"",
                    ""resource_url"": ""https://api.discogs.com/releases/3""
                }
            ]
        }";

        var releaseResponseJson = @"{
            ""title"": ""Collaboration"",
            ""artists"": [
                {
                    ""name"": ""Artist 1"",
                    ""id"": 1
                },
                {
                    ""name"": ""Artist 2"",
                    ""id"": 2
                }
            ]
        }";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/lists/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), listResponseJson, HttpStatusCode.OK));

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/releases/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), releaseResponseJson, HttpStatusCode.OK));

        var releases = Subject.Fetch();

        releases.Should().HaveCount(1);
        releases.First().Artist.Should().Be("Artist 1");
        releases.First().Album.Should().Be("Collaboration");
    }
}
