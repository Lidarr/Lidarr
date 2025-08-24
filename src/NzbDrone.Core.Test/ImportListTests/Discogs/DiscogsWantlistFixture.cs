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
public class DiscogsWantlistFixture : CoreTest<DiscogsWantlist>
{
    [SetUp]
    public void Setup()
    {
        Subject.Definition = new ImportLists.ImportListDefinition
        {
            Id = 1,
            Name = "Test Discogs Wantlist",
            Settings = new DiscogsWantlistSettings
            {
                Token = "test_token_123456",
                Username = "test_user",
                BaseUrl = "https://api.discogs.com"
            }
        };
    }

    [Test]
    public void should_parse_valid_wantlist_response()
    {
        var wantlistResponseJson = @"{
            ""wants"": [
                {
                    ""basic_information"": {
                        ""id"": 3,
                        ""title"": ""Profound Sounds Vol. 1"",
                        ""resource_url"": ""https://api.discogs.com/releases/3"",
                        ""artists"": [
                            {
                                ""name"": ""Josh Wink"",
                                ""id"": 3
                            }
                        ]
                    }
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

        var wantlistCalled = false;
        var releaseCalled = false;
        var actualWantlistUrl = "";
        var actualReleaseUrl = "";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
              .Callback<HttpRequest>(req =>
              {
                  if (req.Url.FullUri.Contains("/wants"))
                  {
                      wantlistCalled = true;
                      actualWantlistUrl = req.Url.FullUri;
                  }
                  else if (req.Url.FullUri.Contains("/releases/"))
                  {
                      releaseCalled = true;
                      actualReleaseUrl = req.Url.FullUri;
                  }
              })
              .Returns<HttpRequest>(req =>
              {
                  if (req.Url.FullUri.Contains("/wants"))
                  {
                      return new HttpResponse(req, new HttpHeader(), wantlistResponseJson, HttpStatusCode.OK);
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
        System.Console.WriteLine($"Wantlist called: {wantlistCalled}, URL: {actualWantlistUrl}");
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
    public void should_handle_empty_wantlist_response()
    {
        var responseJson = @"{""wants"": []}";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
              .Returns(new HttpResponse(new HttpRequest("https://api.discogs.com/users/test_user/wants"), new HttpHeader(), responseJson, HttpStatusCode.OK));

        var releases = Subject.Fetch();

        releases.Should().BeEmpty();
    }

    [Test]
    public void should_skip_items_when_release_fetch_fails()
    {
        var wantlistResponseJson = @"{
            ""wants"": [
                {
                    ""basic_information"": {
                        ""id"": 3,
                        ""title"": ""Profound Sounds Vol. 1"",
                        ""resource_url"": ""https://api.discogs.com/releases/3""
                    }
                }
            ]
        }";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/wants"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), wantlistResponseJson, HttpStatusCode.OK));

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/releases/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), "", HttpStatusCode.NotFound));

        var releases = Subject.Fetch();

        releases.Should().BeEmpty();
    }

    [Test]
    public void should_skip_releases_with_no_artists()
    {
        var wantlistResponseJson = @"{
            ""wants"": [
                {
                    ""basic_information"": {
                        ""id"": 3,
                        ""title"": ""Compilation"",
                        ""resource_url"": ""https://api.discogs.com/releases/3""
                    }
                }
            ]
        }";

        var releaseResponseJson = @"{
            ""title"": ""Compilation"",
            ""artists"": []
        }";

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/wants"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), wantlistResponseJson, HttpStatusCode.OK));

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/releases/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), releaseResponseJson, HttpStatusCode.OK));

        var releases = Subject.Fetch();

        releases.Should().BeEmpty();
    }

    [Test]
    public void should_use_first_artist_when_multiple_artists()
    {
        var wantlistResponseJson = @"{
            ""wants"": [
                {
                    ""basic_information"": {
                        ""id"": 3,
                        ""title"": ""Collaboration"",
                        ""resource_url"": ""https://api.discogs.com/releases/3""
                    }
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
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/wants"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), wantlistResponseJson, HttpStatusCode.OK));

        Mocker.GetMock<IHttpClient>()
              .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/releases/"))))
              .Returns(new HttpResponse(new HttpRequest("http://my.indexer.com"), new HttpHeader(), releaseResponseJson, HttpStatusCode.OK));

        var releases = Subject.Fetch();

        releases.Should().HaveCount(1);
        releases.First().Artist.Should().Be("Artist 1");
        releases.First().Album.Should().Be("Collaboration");
    }
}