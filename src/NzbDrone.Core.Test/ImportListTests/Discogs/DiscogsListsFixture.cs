using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Discogs;
using NzbDrone.Core.ImportLists.Exceptions;

namespace NzbDrone.Core.Test.ImportListTests.Discogs;

[TestFixture]
public class DiscogsListsFixture
{
    private readonly HttpHeader _defaultHeaders = new () { ContentType = "application/json" };

    private DiscogsListsParser _parser;
    private Mock<IHttpClient> _httpClient;
    private DiscogsListsSettings _settings;
    private Logger _logger;

    [SetUp]
    public void SetUp()
    {
        _httpClient = new Mock<IHttpClient>();
        _settings = new DiscogsListsSettings { Token = "token", ListId = "123", BaseUrl = "https://api.discogs.com" };
        _logger = LogManager.GetCurrentClassLogger();
        _parser = new DiscogsListsParser(_settings, _httpClient.Object, _logger);
    }

    [Test]
    public void should_parse_release_items()
    {
        const string resourceUrl = "https://api.discogs.com/releases/3";
        GivenReleaseDetails(resourceUrl, BuildReleaseResponse("Josh Wink", "Profound Sounds Vol. 1"));

        var response = BuildListResponse(@"{
            ""items"": [
                {
                    ""type"": ""release"",
                    ""id"": 3,
                    ""display_title"": ""Josh Wink - Profound Sounds Vol. 1"",
                    ""resource_url"": ""https://api.discogs.com/releases/3""
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().HaveCount(1);
        items.First().Artist.Should().Be("Josh Wink");
        items.First().Album.Should().Be("Profound Sounds Vol. 1");
    }

    [Test]
    public void should_parse_artist_items()
    {
        const string resourceUrl = "https://api.discogs.com/artists/3227";
        GivenArtistDetails(resourceUrl, BuildArtistResponse("Silent Phase"));

        var response = BuildListResponse(@"{
            ""items"": [
                {
                    ""type"": ""artist"",
                    ""id"": 3227,
                    ""display_title"": ""Silent Phase"",
                    ""resource_url"": ""https://api.discogs.com/artists/3227""
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().HaveCount(1);
        items.First().Artist.Should().Be("Silent Phase");
        items.First().Album.Should().BeNull();
    }

    [Test]
    public void should_parse_mixed_release_and_artist_items()
    {
        const string releaseUrl = "https://api.discogs.com/releases/4674";
        const string artistUrl = "https://api.discogs.com/artists/3227";
        GivenReleaseDetails(releaseUrl, BuildReleaseResponse("Silent Phase", "The Rewired Mixes"));
        GivenArtistDetails(artistUrl, BuildArtistResponse("Silent Phase"));

        var response = BuildListResponse(@"{
            ""items"": [
                {
                    ""type"": ""release"",
                    ""id"": 4674,
                    ""display_title"": ""Silent Phase - The Rewired Mixes"",
                    ""resource_url"": ""https://api.discogs.com/releases/4674""
                },
                {
                    ""type"": ""artist"",
                    ""id"": 3227,
                    ""display_title"": ""Silent Phase"",
                    ""resource_url"": ""https://api.discogs.com/artists/3227""
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().HaveCount(2);
        items.First().Artist.Should().Be("Silent Phase");
        items.First().Album.Should().Be("The Rewired Mixes");
        items.Last().Artist.Should().Be("Silent Phase");
        items.Last().Album.Should().BeNull();
    }

    [Test]
    public void should_ignore_non_release_and_non_artist_items()
    {
        var response = BuildListResponse(@"{
            ""items"": [
                {
                    ""type"": ""label"",
                    ""id"": 7,
                    ""display_title"": ""Ignore me"",
                    ""resource_url"": ""https://api.discogs.com/labels/7""
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().BeEmpty();
        _httpClient.Verify(c => c.Execute(It.IsAny<HttpRequest>()), Times.Never);
    }

    [Test]
    public void should_skip_release_when_details_fail()
    {
        var response = BuildListResponse(@"{
            ""items"": [
                {
                    ""type"": ""release"",
                    ""id"": 3,
                    ""display_title"": ""Josh Wink - Profound Sounds Vol. 1"",
                    ""resource_url"": ""https://api.discogs.com/releases/3""
                }
            ]
        }");

        _httpClient.Setup(c => c.Execute(It.IsAny<HttpRequest>()))
            .Returns(new HttpResponse(new HttpRequest("https://api.discogs.com/releases/3"), _defaultHeaders, string.Empty, HttpStatusCode.NotFound));

        var items = _parser.ParseResponse(response);

        items.Should().BeEmpty();
    }

    [Test]
    public void should_skip_artist_when_details_fail()
    {
        var response = BuildListResponse(@"{
            ""items"": [
                {
                    ""type"": ""artist"",
                    ""id"": 3227,
                    ""display_title"": ""Silent Phase"",
                    ""resource_url"": ""https://api.discogs.com/artists/3227""
                }
            ]
        }");

        _httpClient.Setup(c => c.Execute(It.IsAny<HttpRequest>()))
            .Returns(new HttpResponse(new HttpRequest("https://api.discogs.com/artists/3227"), _defaultHeaders, string.Empty, HttpStatusCode.NotFound));

        var items = _parser.ParseResponse(response);

        items.Should().BeEmpty();
    }

    [Test]
    public void should_skip_artist_when_name_is_missing()
    {
        const string resourceUrl = "https://api.discogs.com/artists/3227";
        GivenArtistDetails(resourceUrl, @"{ ""id"": 3227 }");

        var response = BuildListResponse(@"{
            ""items"": [
                {
                    ""type"": ""artist"",
                    ""id"": 3227,
                    ""display_title"": ""Silent Phase"",
                    ""resource_url"": ""https://api.discogs.com/artists/3227""
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().BeEmpty();
    }

    [Test]
    public void should_throw_when_discogs_returns_html()
    {
        var response = BuildListResponse("<html></html>", contentType: "text/html");

        _parser.Invoking(p => p.ParseResponse(response))
            .Should().Throw<ImportListException>()
            .WithMessage("*HTML content*");
    }

    private ImportListResponse BuildListResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = "application/json")
    {
        var httpRequest = new HttpRequest("https://api.discogs.com/lists/123");
        var importListRequest = new ImportListRequest(httpRequest);
        var headers = new HttpHeader { ContentType = contentType };
        var httpResponse = new HttpResponse(httpRequest, headers, content, statusCode);

        return new ImportListResponse(importListRequest, httpResponse);
    }

    private void GivenReleaseDetails(string resourceUrl, string payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _httpClient.Setup(c => c.Execute(It.Is<HttpRequest>(r => r.Url.FullUri == resourceUrl)))
            .Returns(new HttpResponse(new HttpRequest(resourceUrl), _defaultHeaders, payload, statusCode));
    }

    private void GivenArtistDetails(string resourceUrl, string payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _httpClient.Setup(c => c.Execute(It.Is<HttpRequest>(r => r.Url.FullUri == resourceUrl)))
            .Returns(new HttpResponse(new HttpRequest(resourceUrl), _defaultHeaders, payload, statusCode));
    }

    private static string BuildReleaseResponse(string artist, string title)
    {
        return $@"{{
            ""title"": ""{title}"",
            ""artists"": [
                {{ ""name"": ""{artist}"", ""id"": 3 }}
            ]
        }}";
    }

    private static string BuildArtistResponse(string name)
    {
        return $@"{{
            ""name"": ""{name}"",
            ""id"": 3227
        }}";
    }
}
