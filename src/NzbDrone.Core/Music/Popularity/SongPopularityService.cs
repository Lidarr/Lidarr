using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Music.Popularity
{
    public interface ISongPopularityService
    {
        Task<SongPopularityResult> GetArtistPopularity(Artist artist);
    }

    public class SongPopularityService : ISongPopularityService
    {
        private readonly IHttpClient _httpClient;
        private readonly ITrackService _trackService;
        private readonly ICached<SongPopularityResult> _cache;
        private readonly SemaphoreSlim _fetchLock = new SemaphoreSlim(1, 1);
        private readonly Logger _logger;

        public SongPopularityService(IHttpClient httpClient, ITrackService trackService, ICacheManager cacheManager, Logger logger)
        {
            _httpClient = httpClient;
            _trackService = trackService;
            _cache = cacheManager.GetCache<SongPopularityResult>(GetType());
            _logger = logger;
        }

        public async Task<SongPopularityResult> GetArtistPopularity(Artist artist)
        {
            var key = artist.ForeignArtistId;
            var cached = _cache.Find(key);
            if (cached != null)
            {
                return cached;
            }

            await _fetchLock.WaitAsync();
            try
            {
                cached = _cache.Find(key);
                if (cached != null)
                {
                    return cached;
                }

                SongPopularityResult result;
                try
                {
                    result = await Fetch(artist);
                }
                catch (Exception exception)
                {
                    // Optional discovery data must never block monitoring or downloads.
                    _logger.Debug(exception, "Deezer song popularity is temporarily unavailable");
                    result = new SongPopularityResult { Status = "unavailable" };
                }

                result.FetchedAt = DateTime.UtcNow;
                _cache.Set(key, result, result.Status == "unavailable" ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(12));
                return result;
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        private async Task<SongPopularityResult> Fetch(Artist artist)
        {
            var localTracks = _trackService.GetTracksByArtist(artist.Id)
                .Where(track => !string.IsNullOrWhiteSpace(track.ForeignRecordingId) && track.Duration > 0)
                .ToList();
            if (localTracks.Count == 0)
            {
                return new SongPopularityResult();
            }

            var linkedId = GetLinkedArtistId(artist);
            if (linkedId.HasValue)
            {
                var linkedTracks = await GetPage<DeezerTrack>("artist/" + linkedId.Value + "/top", null);
                return ResultFor(Map(localTracks, linkedTracks));
            }

            var candidates = (await GetPage<DeezerArtist>("search/artist", artist.Name))
                .Where(candidate => candidate.Id > 0 && Normalize(candidate.Name) == Normalize(artist.Name))
                .OrderByDescending(candidate => candidate.Fans)
                .DistinctBy(candidate => candidate.Id)
                .Take(3)
                .ToList();
            var matches = new List<(int Evidence, List<SongPopularity> Tracks)>();
            foreach (var candidate in candidates)
            {
                var topTracks = await GetPage<DeezerTrack>("artist/" + candidate.Id + "/top", null);
                var mapped = Map(localTracks, topTracks);

                // Names alone are not enough: multiple performers can share a name.
                var evidence = Map(localTracks, topTracks.Where(track => track.Artist?.Id == candidate.Id).ToList()).Count;
                if (evidence >= 2)
                {
                    matches.Add((evidence, mapped));
                }
            }

            var ordered = matches.OrderByDescending(match => match.Evidence).ToList();
            if (ordered.Count == 0 || (ordered.Count > 1 && ordered[0].Evidence == ordered[1].Evidence))
            {
                return new SongPopularityResult();
            }

            return ResultFor(ordered[0].Tracks);
        }

        private async Task<List<T>> GetPage<T>(string path, string query)
        {
            // Only these fixed, public catalog endpoints are contacted. Never follow
            // provider-supplied links, pagination URLs, audio previews, or redirects.
            var builder = new HttpRequestBuilder("https://api.deezer.com/")
                .Resource(path)
                .AddQueryParam("limit", query == null ? 100 : 10);
            if (query != null)
            {
                builder.AddQueryParam("q", query);
            }

            var request = builder.Build();
            request.RequestTimeout = TimeSpan.FromSeconds(8);
            request.AllowAutoRedirect = false;
            var response = await _httpClient.GetAsync(request);
            if (response.HasHttpError || response.HasHttpRedirect)
            {
                throw new HttpException(request, response);
            }

            var page = Json.Deserialize<DeezerPage<T>>(response.Content);
            if (page.Error.HasValue)
            {
                throw new InvalidOperationException("The popularity provider returned a catalog error");
            }

            return page.Data ?? new List<T>();
        }

        private static SongPopularityResult ResultFor(List<SongPopularity> tracks)
        {
            return new SongPopularityResult { Status = tracks.Count > 0 ? "available" : "noMatch", Tracks = tracks };
        }

        private static List<SongPopularity> Map(List<Track> localTracks, List<DeezerTrack> providerTracks)
        {
            var result = new Dictionary<string, SongPopularity>();
            for (var index = 0; index < providerTracks.Count; index++)
            {
                var remote = providerTracks[index];
                if (remote.Id <= 0 || remote.Duration <= 0)
                {
                    continue;
                }

                var title = remote.Title ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(remote.Version) && !Normalize(title).EndsWith(Normalize(remote.Version), StringComparison.Ordinal))
                {
                    title += " " + remote.Version;
                }

                var normalized = Normalize(title);
                var ids = localTracks.Where(track => Normalize(track.Title) == normalized &&
                        Math.Abs((long)track.Duration - (remote.Duration * 1000L)) <= 5000)
                    .Select(track => track.ForeignRecordingId).Distinct().ToList();

                // Do not assign a chart position to an ambiguous recording/version.
                if (ids.Count != 1)
                {
                    continue;
                }

                result.TryAdd(ids[0], new SongPopularity
                {
                    ForeignRecordingId = ids[0],
                    Position = index + 1,
                    ProviderScore = Math.Max(0, remote.Rank),
                    SourceUrl = "https://www.deezer.com/track/" + remote.Id.ToString(CultureInfo.InvariantCulture)
                });
            }

            return result.Values.OrderBy(track => track.Position).ToList();
        }

        private static long? GetLinkedArtistId(Artist artist)
        {
            foreach (var link in artist.Metadata.Value.Links ?? new List<Links>())
            {
                if (Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) &&
                    (uri.Host.Equals("deezer.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".deezer.com", StringComparison.OrdinalIgnoreCase)))
                {
                    var match = Regex.Match(uri.AbsolutePath, @"^/(?:[a-z]{2}/)?artist/([0-9]+)/?$", RegexOptions.IgnoreCase);
                    if (match.Success && long.TryParse(match.Groups[1].Value, out var id) && id > 0)
                    {
                        return id;
                    }
                }
            }

            return null;
        }

        private static string Normalize(string value)
        {
            var title = Regex.Replace(value ?? string.Empty, @"\s*[\(\[]\s*(?:feat\.?|ft\.?|featuring)\s+[^\)\]]*[\)\]]", string.Empty, RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\s+(?:feat\.|ft\.|featuring)\s+.*$", string.Empty, RegexOptions.IgnoreCase);
            return Regex.Replace(title.Normalize(NormalizationForm.FormKC).ToLowerInvariant(), @"[^\p{L}\p{Nd}]", string.Empty);
        }

        public class DeezerPage<T>
        {
            public List<T> Data { get; set; }
            public JsonElement? Error { get; set; }
        }

        public class DeezerArtist
        {
            public long Id { get; set; }
            public string Name { get; set; }
            [JsonPropertyName("nb_fan")]
            public long Fans { get; set; }
        }

        public class DeezerTrack
        {
            public long Id { get; set; }
            public string Title { get; set; }
            [JsonPropertyName("title_version")]
            public string Version { get; set; }
            public int Duration { get; set; }
            public long Rank { get; set; }
            public DeezerArtist Artist { get; set; }
        }
    }
}
