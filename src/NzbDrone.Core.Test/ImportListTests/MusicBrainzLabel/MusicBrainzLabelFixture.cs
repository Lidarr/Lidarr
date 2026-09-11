using System.IO;
using System.Linq;
using System.Net;
using FluentAssertions;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.MusicBrainzLabel;

namespace NzbDrone.Core.Test.ImportListTests.MusicBrainzLabel;

[TestFixture]
public class MusicBrainzLabelFixture
{
    private const string VariousArtistsId = "89ad4ac3-39f7-470e-963a-56509c546377";

    private MusicBrainzLabelSettings _settings;

    [SetUp]
    public void SetUp()
    {
        _settings = new MusicBrainzLabelSettings
        {
            LabelId = "a539bb1e-f2e1-4b45-9db8-8053841e7503"
        };
    }

    private MusicBrainzLabelParser Parser()
    {
        return new MusicBrainzLabelParser(_settings, LogManager.GetCurrentClassLogger());
    }

    [Test]
    public void should_parse_album_with_musicbrainz_ids()
    {
        var items = Parser().ParseResponse(PageOf(Release()));

        items.Should().HaveCount(1);

        var item = items.First();
        item.Artist.Should().Be("Cocteau Twins");
        item.ArtistMusicBrainzId.Should().Be("000fc734-b7e1-4a01-92d1-f544261b43f5");
        item.Album.Should().Be("Treasure");
        item.AlbumMusicBrainzId.Should().Be("11111111-1111-1111-1111-111111111111");
        item.ReleaseDate.Year.Should().Be(1984);
    }

    [Test]
    public void should_skip_types_that_are_not_selected()
    {
        // Singles are off by default.
        var items = Parser().ParseResponse(PageOf(Release(primaryType: "Single")));

        items.Should().BeEmpty();
    }

    [Test]
    public void should_skip_excluded_secondary_types()
    {
        var items = Parser().ParseResponse(PageOf(Release(secondaryTypes: @"""Compilation""")));

        items.Should().BeEmpty();
    }

    [Test]
    public void should_keep_secondary_types_that_are_not_excluded()
    {
        var items = Parser().ParseResponse(PageOf(Release(secondaryTypes: @"""Soundtrack""")));

        items.Should().HaveCount(1);
    }

    [Test]
    public void should_skip_various_artists()
    {
        var items = Parser().ParseResponse(PageOf(Release(artistId: VariousArtistsId, artistName: "Various Artists")));

        items.Should().BeEmpty();
    }

    [Test]
    public void should_keep_various_artists_when_not_excluded()
    {
        _settings.ExcludeVariousArtists = false;

        var items = Parser().ParseResponse(PageOf(Release(artistId: VariousArtistsId, artistName: "Various Artists")));

        items.Should().HaveCount(1);
    }

    [Test]
    public void should_skip_non_official_releases()
    {
        var items = Parser().ParseResponse(PageOf(Release(status: "Promotion")));

        items.Should().BeEmpty();
    }

    [Test]
    public void should_accept_official_pressing_that_follows_a_promo()
    {
        // Status is a property of the release, not the release group, so a promo
        // must not poison the group for the official pressing behind it.
        var items = Parser().ParseResponse(PageOf(
            Release(status: "Promotion"),
            Release(status: "Official")));

        items.Should().HaveCount(1);
    }

    [Test]
    public void should_collapse_multiple_pressings_of_one_release_group()
    {
        var items = Parser().ParseResponse(PageOf(
            Release(title: "Treasure"),
            Release(title: "Treasure (reissue)"),
            Release(title: "Treasure (Japanese pressing)")));

        items.Should().HaveCount(1);
    }

    [Test]
    public void should_dedupe_release_groups_across_pages()
    {
        // One parser serves every page of a fetch, so a release group split over
        // a page boundary must still only produce one item.
        var parser = Parser();

        parser.ParseResponse(PageOf(Release())).Should().HaveCount(1);
        parser.ParseResponse(PageOf(Release())).Should().BeEmpty();
    }

    [Test]
    public void should_skip_albums_before_minimum_year()
    {
        _settings.MinimumYear = 1990;

        var items = Parser().ParseResponse(PageOf(Release(date: "1984-10-01")));

        items.Should().BeEmpty();
    }

    [Test]
    public void should_keep_albums_on_or_after_minimum_year()
    {
        _settings.MinimumYear = 1990;

        var items = Parser().ParseResponse(PageOf(Release(date: "1990")));

        items.Should().HaveCount(1);
    }

    [Test]
    public void should_keep_undated_albums_when_filtering_by_year()
    {
        // An undated release group is usually forthcoming, not ancient.
        _settings.MinimumYear = 1990;

        var items = Parser().ParseResponse(PageOf(Release(date: null)));

        items.Should().HaveCount(1);
    }

    [TestCase("1984", 1984)]
    [TestCase("1984-10", 1984)]
    [TestCase("1984-10-01", 1984)]
    public void should_parse_partial_dates(string date, int expectedYear)
    {
        var items = Parser().ParseResponse(PageOf(Release(date: date)));

        items.Should().HaveCount(1);
        items.First().ReleaseDate.Year.Should().Be(expectedYear);
    }

    [Test]
    public void should_skip_releases_without_a_release_group()
    {
        var items = Parser().ParseResponse(PageOf(@"{ ""id"": ""r1"", ""title"": ""Orphan"", ""status"": ""Official"" }"));

        items.Should().BeEmpty();
    }

    [Test]
    public void should_throw_when_musicbrainz_returns_html()
    {
        var response = Response("<html></html>", contentType: "text/html");

        Parser().Invoking(p => p.ParseResponse(response))
            .Should().Throw<ImportListException>()
            .WithMessage("*HTML content*");
    }

    [Test]
    public void should_throw_on_error_status_code()
    {
        var response = Response("{}", statusCode: HttpStatusCode.ServiceUnavailable);

        Parser().Invoking(p => p.ParseResponse(response))
            .Should().Throw<ImportListException>();
    }

    [Test]
    public void should_parse_a_real_musicbrainz_response()
    {
        // Hand-written JSON only proves the parser agrees with itself. This is an
        // untouched browse response for Ghost Box (25 of its 126 releases), so a
        // wrong field name or nesting level shows up here rather than in
        // production.
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "ImportLists", "MusicBrainzLabelGhostBox.json");

        var items = Parser().ParseResponse(Response(File.ReadAllText(path)));

        // 25 releases collapse to 14 albums once pressings are folded together and
        // the default type filters are applied.
        items.Should().HaveCount(14);
        items.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i.Artist));
        items.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i.Album));
        items.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i.ArtistMusicBrainzId));
        items.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i.AlbumMusicBrainzId));

        items.Should().Contain(i => i.Artist == "Belbury Poly" && i.Album == "The Owl's Map");
        items.Should().Contain(i => i.Artist == "The Advisory Circle" && i.Album == "Other Channels");

        // Singles and compilations are filtered out by default.
        items.Should().NotContain(i => i.Album == "The Hidden Door");
        items.Should().NotContain(i => i.Album == "Ritual and Education");
    }

    [TestCase("a539bb1e-f2e1-4b45-9db8-8053841e7503", "a539bb1e-f2e1-4b45-9db8-8053841e7503")]
    [TestCase("https://musicbrainz.org/label/a539bb1e-f2e1-4b45-9db8-8053841e7503", "a539bb1e-f2e1-4b45-9db8-8053841e7503")]
    [TestCase("A539BB1E-F2E1-4B45-9DB8-8053841E7503", "a539bb1e-f2e1-4b45-9db8-8053841e7503")]
    [TestCase("not-a-label", null)]
    [TestCase("", null)]
    [TestCase(null, null)]
    public void should_parse_label_id_from_input(string input, string expected)
    {
        MusicBrainzLabelSettings.ParseLabelId(input).Should().Be(expected);
    }

    private static string Release(string title = "Treasure",
        string primaryType = "Album",
        string secondaryTypes = "",
        string status = "Official",
        string date = "1984-10-01",
        string artistId = "000fc734-b7e1-4a01-92d1-f544261b43f5",
        string artistName = "Cocteau Twins",
        string releaseGroupId = "11111111-1111-1111-1111-111111111111")
    {
        var dateJson = date == null ? "null" : $@"""{date}""";

        return $@"{{
            ""id"": ""aaaaaaaa-0000-0000-0000-000000000000"",
            ""title"": ""{title}"",
            ""status"": ""{status}"",
            ""release-group"": {{
                ""id"": ""{releaseGroupId}"",
                ""title"": ""Treasure"",
                ""primary-type"": ""{primaryType}"",
                ""secondary-types"": [{secondaryTypes}],
                ""first-release-date"": {dateJson},
                ""artist-credit"": [
                    {{ ""name"": ""{artistName}"", ""artist"": {{ ""id"": ""{artistId}"", ""name"": ""{artistName}"" }} }}
                ]
            }}
        }}";
    }

    private static ImportListResponse PageOf(params string[] releases)
    {
        return Response($@"{{ ""release-count"": {releases.Length}, ""releases"": [{string.Join(",", releases)}] }}");
    }

    private static ImportListResponse Response(string content,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string contentType = "application/json")
    {
        var httpRequest = new HttpRequest("https://musicbrainz.org/ws/2/release");
        var importListRequest = new ImportListRequest(httpRequest);
        var headers = new HttpHeader { ContentType = contentType };
        var httpResponse = new HttpResponse(httpRequest, headers, content, statusCode);

        return new ImportListResponse(importListRequest, httpResponse);
    }
}
