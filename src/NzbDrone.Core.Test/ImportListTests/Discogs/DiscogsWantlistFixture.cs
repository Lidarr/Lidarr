using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Discogs;
using NzbDrone.Core.ImportLists.Exceptions;

namespace NzbDrone.Core.Test.ImportListTests.Discogs;

[TestFixture]
public class DiscogsWantlistFixture
{
    private readonly HttpHeader _defaultHeaders = new () { ContentType = "application/json" };

    private DiscogsWantlistParser _parser;
    private Mock<IHttpClient> _httpClient;
    private DiscogsWantlistSettings _settings;

    [SetUp]
    public void SetUp()
    {
        _httpClient = new Mock<IHttpClient>();
        _parser = new DiscogsWantlistParser();
        _settings = new DiscogsWantlistSettings { Token = "token", Username = "user", BaseUrl = "https://api.discogs.com" };

        _parser.SetContext(_httpClient.Object, _settings);
    }

    [Test]
    public void should_parse_wantlist_items()
    {
        var response = BuildWantlistResponse(@"{
            ""wants"": [
                {
                    ""basic_information"": {
                        ""id"": 3,
                        ""title"": ""Profound Sounds Vol. 1"",
                        ""resource_url"": ""https://api.discogs.com/releases/3"",
                        ""artists"": [
                            { ""name"": ""Josh Wink"", ""id"": 123 }
                        ]
                    }
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().HaveCount(1);
        items.First().Artist.Should().Be("Josh Wink");
        items.First().Album.Should().Be("Profound Sounds Vol. 1");
        _httpClient.Verify(c => c.Execute(It.IsAny<HttpRequest>()), Times.Never);
    }

    [Test]
    public void should_skip_entries_without_artists()
    {
        var response = BuildWantlistResponse(@"{
            ""wants"": [
                {
                    ""basic_information"": {
                        ""id"": 3,
                        ""title"": ""Missing artists""
                    }
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().BeEmpty();
        _httpClient.Verify(c => c.Execute(It.IsAny<HttpRequest>()), Times.Never);
    }

    [Test]
    public void should_skip_entries_without_title()
    {
        var response = BuildWantlistResponse(@"{
            ""wants"": [
                {
                    ""basic_information"": {
                        ""id"": 3,
                        ""artists"": [
                            { ""name"": ""Test Artist"", ""id"": 123 }
                        ]
                    }
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().BeEmpty();
        _httpClient.Verify(c => c.Execute(It.IsAny<HttpRequest>()), Times.Never);
    }

    [Test]
    public void should_skip_entries_with_null_basic_information()
    {
        var response = BuildWantlistResponse(@"{
            ""wants"": [
                {
                    ""basic_information"": null
                }
            ]
        }");

        var items = _parser.ParseResponse(response);

        items.Should().BeEmpty();
        _httpClient.Verify(c => c.Execute(It.IsAny<HttpRequest>()), Times.Never);
    }

    [Test]
    public void should_throw_when_discogs_returns_html()
    {
        var response = BuildWantlistResponse("<html></html>", contentType: "text/html");

        _parser.Invoking(p => p.ParseResponse(response))
            .Should().Throw<ImportListException>()
            .WithMessage("*HTML content*");
    }

    private ImportListResponse BuildWantlistResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = "application/json")
    {
        var httpRequest = new HttpRequest("https://api.discogs.com/users/user/wants");
        var importListRequest = new ImportListRequest(httpRequest);
        var headers = new HttpHeader { ContentType = contentType };
        var httpResponse = new HttpResponse(httpRequest, headers, content, statusCode);

        return new ImportListResponse(importListRequest, httpResponse);
    }

}
