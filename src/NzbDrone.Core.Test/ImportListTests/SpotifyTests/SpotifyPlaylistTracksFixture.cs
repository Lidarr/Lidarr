using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Spotify;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.SpotifyTests
{
    [TestFixture]
    public class SpotifyPlaylistTracksFixture : CoreTest<SpotifyPlaylistTracks>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new ImportListDefinition()
            {
                Name = "Spotify playlist tracks",
                Settings = new SpotifyPlaylistTracksSettings()
                {
                    ClientId = "abc",
                    ClientSecret = "xyz",
                    BaseUrl = "SpotifyBaseUrl",
                    Count = 410,
                    PlaylistId = "playlistID"
                }
            };
        }

        [Test]
        public void should_have_default_page_size_of_100()
        {
            Subject.PageSize.Should().Be(100);
        }

        [Test]
        public void should_parse_albums_and_artists_from_spotifyplaylisttracks_response_and_make_a_single_call_if_one_page()
        {
            var tokenResponse = ReadAllText(@"Files/ImportLists/Spotify/SpotifyToken.json");
            var base64ClientIdAndSecret = "YWJjOnh5eg==";
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.POST && r.Url.FullUri == "https://accounts.spotify.com/api/token" && r.Headers["Authorization"] == $"Basic {base64ClientIdAndSecret}")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tokenResponse));
            
            var tracksResponse = ReadAllText(@"Files/ImportLists/Spotify/SpotifyPlaylistTracksNotFull.json");
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=0" && r.Headers["Authorization"] == "Bearer the_token")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tracksResponse));

            var importListResults = Subject.Fetch();

            importListResults.Should().HaveCount(10);

            importListResults.First().Should().BeOfType<ImportListItemInfo>();
            var firstResult = importListResults.First() as ImportListItemInfo;

            firstResult.Artist.Should().Be("Tanooki Suit");
            firstResult.Album.Should().Be("Euclid");
        }

        [Test]
        public void should_cleanup_duplicates()
        {
            var tokenResponse = ReadAllText(@"Files/ImportLists/Spotify/SpotifyToken.json");
            var base64ClientIdAndSecret = "YWJjOnh5eg==";
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.POST && r.Url.FullUri == "https://accounts.spotify.com/api/token" && r.Headers["Authorization"] == $"Basic {base64ClientIdAndSecret}")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tokenResponse));

            var tracksResponseFull = ReadAllText(@"Files/ImportLists/Spotify/SpotifyPlaylistTracksFull.json");
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=0" && r.Headers["Authorization"] == "Bearer the_token")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tracksResponseFull));

            var tracksResponseNotFull = ReadAllText(@"Files/ImportLists/Spotify/SpotifyPlaylistTracksNotFull.json");
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=100" && r.Headers["Authorization"] == "Bearer the_token")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tracksResponseNotFull));

            var importListResults = Subject.Fetch();

            importListResults.Should().HaveCount(10); // SpotifyPlaylistTracksFull contains the same 10 results as SpotifyPlaylistTracksNotFull repeated 10 times
        }

        [Test]
        public void should_make_multiple_calls_if_multiple_pages_and_calls_token_only_once()
        {
            var tokenResponse = ReadAllText(@"Files/ImportLists/Spotify/SpotifyToken.json");
            var base64ClientIdAndSecret = "YWJjOnh5eg==";
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.POST && r.Url.FullUri == "https://accounts.spotify.com/api/token" && r.Headers["Authorization"] == $"Basic {base64ClientIdAndSecret}")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tokenResponse));

            var tracksResponseFull = ReadAllText(@"Files/ImportLists/Spotify/SpotifyPlaylistTracksFull.json");
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=0" && r.Headers["Authorization"] == "Bearer the_token")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tracksResponseFull));
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=100" && r.Headers["Authorization"] == "Bearer the_token")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tracksResponseFull));
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=200" && r.Headers["Authorization"] == "Bearer the_token")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tracksResponseFull));
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=300" && r.Headers["Authorization"] == "Bearer the_token")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tracksResponseFull));

            var tracksResponseNotFull = ReadAllText(@"Files/ImportLists/Spotify/SpotifyPlaylistTracksNotFull.json");
            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=10&offset=400" && r.Headers["Authorization"] == "Bearer the_token")))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), tracksResponseNotFull));

            var importListResults = Subject.Fetch();

            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Execute(It.IsAny<HttpRequest>()), Times.Exactly(6));
            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.POST && r.Url.FullUri == "https://accounts.spotify.com/api/token" && r.Headers["Authorization"] == $"Basic {base64ClientIdAndSecret}")));
            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=0" && r.Headers["Authorization"] == "Bearer the_token")));
            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=100" && r.Headers["Authorization"] == "Bearer the_token")));
            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=200" && r.Headers["Authorization"] == "Bearer the_token")));
            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=100&offset=300" && r.Headers["Authorization"] == "Bearer the_token")));
            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Method == HttpMethod.GET && r.Url.FullUri == "SpotifyBaseUrl/playlists/playlistID/tracks?fields=items(track(name,album(name,id),artists))&limit=10&offset=400" && r.Headers["Authorization"] == "Bearer the_token")));
        }
    }
}
