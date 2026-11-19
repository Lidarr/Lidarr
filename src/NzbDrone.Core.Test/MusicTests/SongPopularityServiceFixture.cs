using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Popularity;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class SongPopularityServiceFixture : CoreTest<SongPopularityService>
    {
        private Artist _artist;
        private List<Track> _tracks;
        private Dictionary<string, string> _pages;
        private List<HttpRequest> _requests;

        [SetUp]
        public void Setup()
        {
            _artist = new Artist { Id = 12, Name = "Example Artist", ForeignArtistId = "artist-recording-owner" };
            _tracks = new List<Track>
            {
                new Track { ForeignRecordingId = "first", Title = "First Song", Duration = 180000 },
                new Track { ForeignRecordingId = "second", Title = "Second Song", Duration = 210000 }
            };
            _pages = new Dictionary<string, string>
            {
                ["/search/artist"] = """{"data":[{"id":42,"name":"Example Artist","nb_fan":1000}]}""",
                ["/artist/42/top"] = """{"data":[{"id":101,"title":"First Song","duration":180,"rank":800000,"artist":{"id":42}},{"id":102,"title":"Second Song","duration":210,"rank":700000,"artist":{"id":42}}]}"""
            };
            _requests = new List<HttpRequest>();
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());
            Mocker.GetMock<ITrackService>().Setup(service => service.GetTracksByArtist(_artist.Id)).Returns(_tracks);
            Mocker.GetMock<IHttpClient>().Setup(client => client.GetAsync(It.IsAny<HttpRequest>()))
                .ReturnsAsync((HttpRequest request) =>
                {
                    _requests.Add(request);
                    return new HttpResponse(request, new HttpHeader(), _pages[new Uri(request.Url.FullUri).AbsolutePath]);
                });
        }

        [Test]
        public async Task should_match_an_artist_using_multiple_recordings_and_cache_the_result()
        {
            var result = await Subject.GetArtistPopularity(_artist);
            var repeated = await Subject.GetArtistPopularity(_artist);

            result.Status.Should().Be("available");
            result.Source.Should().Be("Deezer");
            result.FetchedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.Tracks.Should().HaveCount(2);
            result.Tracks[0].ForeignRecordingId.Should().Be("first");
            result.Tracks[0].Position.Should().Be(1);
            result.Tracks[0].ProviderScore.Should().Be(800000);
            result.Tracks[0].SourceUrl.Should().Be("https://www.deezer.com/track/101");
            repeated.Should().BeSameAs(result);
            _requests.Should().HaveCount(2);
            _requests.Should().OnlyContain(request => request.Url.Host == "api.deezer.com" && !request.AllowAutoRedirect);
        }

        [Test]
        public async Task should_require_more_than_a_shared_artist_name_and_one_song()
        {
            _tracks.RemoveAt(1);

            var result = await Subject.GetArtistPopularity(_artist);

            result.Status.Should().Be("noMatch");
            result.Tracks.Should().BeEmpty();
        }

        [Test]
        public async Task should_use_an_explicit_artist_catalog_link_without_guessing_from_the_name()
        {
            _artist.Metadata.Value.Links.Add(new Links { Url = "https://www.deezer.com/en/artist/42" });
            _tracks.RemoveAt(1);

            var result = await Subject.GetArtistPopularity(_artist);

            result.Tracks.Should().ContainSingle();
            _requests.Should().ContainSingle(request => new Uri(request.Url.FullUri).AbsolutePath == "/artist/42/top");
        }

        [TestCase("First Song (Live)", 180000)]
        [TestCase("First Song (Remix)", 180000)]
        [TestCase("First Song", 190000)]
        public async Task should_not_confuse_versions_or_different_durations(string title, int duration)
        {
            _tracks[0].Title = title;
            _tracks[0].Duration = duration;
            _artist.Metadata.Value.Links.Add(new Links { Url = "https://deezer.com/artist/42" });

            var result = await Subject.GetArtistPopularity(_artist);

            result.Tracks.Should().OnlyContain(track => track.ForeignRecordingId == "second");
        }

        [Test]
        public async Task should_match_feature_credits_and_preserve_the_provider_position()
        {
            _artist.Metadata.Value.Links.Add(new Links { Url = "https://deezer.com/artist/42" });
            _pages["/artist/42/top"] = """{"data":[{"id":99,"title":"Other Song","duration":200},{"id":101,"title":"First Song (feat. Someone)","duration":183,"rank":800000}]}""";

            var result = await Subject.GetArtistPopularity(_artist);

            result.Tracks.Should().ContainSingle();
            result.Tracks[0].Position.Should().Be(2);
            result.Tracks[0].ForeignRecordingId.Should().Be("first");
        }

        [Test]
        public async Task should_not_assign_a_position_to_ambiguous_recordings()
        {
            _artist.Metadata.Value.Links.Add(new Links { Url = "https://deezer.com/artist/42" });
            _tracks.Add(new Track { ForeignRecordingId = "different-recording", Title = "First Song", Duration = 181000 });

            var result = await Subject.GetArtistPopularity(_artist);

            result.Tracks.Should().ContainSingle(track => track.ForeignRecordingId == "second");
        }

        [Test]
        public async Task should_group_album_appearances_of_the_same_recording()
        {
            _tracks.Add(new Track { ForeignRecordingId = "first", Title = "First Song", Duration = 181000 });

            var result = await Subject.GetArtistPopularity(_artist);

            result.Tracks.Should().HaveCount(2);
        }

        [Test]
        public async Task should_reject_equally_matching_artists_instead_of_choosing_the_one_with_more_fans()
        {
            _pages["/search/artist"] = """{"data":[{"id":42,"name":"Example Artist","nb_fan":1000},{"id":43,"name":"Example Artist","nb_fan":1}]}""";
            _pages["/artist/43/top"] = _pages["/artist/42/top"].Replace("\"id\":42", "\"id\":43");

            var result = await Subject.GetArtistPopularity(_artist);

            result.Status.Should().Be("noMatch");
        }

        [Test]
        public async Task should_ignore_fuzzy_artist_names_and_provider_links()
        {
            _pages["/search/artist"] = """{"data":[{"id":42,"name":"Example Artist Tribute"}],"next":"https://untrusted.example/next"}""";

            var result = await Subject.GetArtistPopularity(_artist);

            result.Tracks.Should().BeEmpty();
            _requests.Should().ContainSingle();
        }

        [TestCase(HttpStatusCode.TooManyRequests)]
        [TestCase(HttpStatusCode.Redirect)]
        public async Task should_tolerate_and_cache_provider_failures(HttpStatusCode status)
        {
            Mocker.GetMock<IHttpClient>().Setup(client => client.GetAsync(It.IsAny<HttpRequest>()))
                .ReturnsAsync((HttpRequest request) => new HttpResponse(request, new HttpHeader(), "{}", status));

            var result = await Subject.GetArtistPopularity(_artist);
            await Subject.GetArtistPopularity(_artist);

            result.Status.Should().Be("unavailable");
            result.Tracks.Should().BeEmpty();
            Mocker.GetMock<IHttpClient>().Verify(client => client.GetAsync(It.IsAny<HttpRequest>()), Times.Once());
        }

        [Test]
        public async Task should_not_contact_the_provider_when_there_are_no_local_recordings()
        {
            _tracks.Clear();

            var result = await Subject.GetArtistPopularity(_artist);

            result.Status.Should().Be("noMatch");
            _requests.Should().BeEmpty();
        }
    }
}
