using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Identification
{
    public interface IOllamaTrackMatcher
    {
        bool IsEnabled { get; }
        bool RequireEqualTrackCount { get; }
        double MinimumScore { get; }
        OllamaTrackMatchResult Match(LocalTrack localTrack, Track candidateTrack, double currentScore);
    }

    public class OllamaTrackMatchResult
    {
        public bool IsMatch { get; set; }
        public double Confidence { get; set; }
        public string Reason { get; set; }
    }

    public class OllamaTrackMatcher : IOllamaTrackMatcher
    {
        private const string DefaultUrl = "http://192.168.2.150:11434";
        private const string DefaultModel = "qwen3";
        private const double DefaultMinimumScore = 0.80;
        private const int DefaultTimeoutSeconds = 10;
        private const int DefaultNumPredict = 64;
        private const string DefaultKeepAlive = "-1m";

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public OllamaTrackMatcher(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool IsEnabled => GetBool("LIDARR_OLLAMA_MATCHING_ENABLED", true);
        public bool RequireEqualTrackCount => GetBool("LIDARR_OLLAMA_REQUIRE_EQUAL_TRACK_COUNT", true);
        public double MinimumScore => GetDouble("LIDARR_OLLAMA_MIN_SCORE", DefaultMinimumScore);

        public OllamaTrackMatchResult Match(LocalTrack localTrack, Track candidateTrack, double currentScore)
        {
            if (!IsEnabled)
            {
                _logger.Info("Ollama track matcher is disabled by LIDARR_OLLAMA_MATCHING_ENABLED");
                return NoMatch("disabled");
            }

            try
            {
                var model = GetModel();
                var url = GetUrl();

                _logger.Info("Ollama track matcher checking low-confidence track match with {0} at {1}: score {2:P1}, local '{3}', candidate '{4}'",
                             model,
                             url,
                             currentScore,
                             localTrack.FileTrackInfo?.Title ?? localTrack.Path,
                             candidateTrack.Title);

                var request = new HttpRequestBuilder(url)
                    .Resource("/api/generate")
                    .Post()
                    .Accept(HttpAccept.Json)
                    .Build();

                request.Headers.ContentType = "application/json";
                request.RequestTimeout = TimeSpan.FromSeconds(GetInt("LIDARR_OLLAMA_TIMEOUT_SECONDS", DefaultTimeoutSeconds));
                request.SuppressHttpError = true;
                request.SuppressHttpErrorStatusCodes = new[] { HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable };
                request.SetContent(new OllamaGenerateRequest
                {
                    Model = model,
                    Prompt = BuildPrompt(localTrack, candidateTrack, currentScore),
                    Stream = false,
                    Format = "json",
                    Think = GetBool("LIDARR_OLLAMA_THINKING_ENABLED", false),
                    KeepAlive = GetKeepAlive(),
                    Options = new OllamaGenerateOptions
                    {
                        Temperature = 0.0,
                        NumPredict = GetInt("LIDARR_OLLAMA_NUM_PREDICT", DefaultNumPredict)
                    }
                }.ToJson());

                var response = _httpClient.Post<OllamaGenerateResponse>(request);
                if (response.HasHttpError)
                {
                    _logger.Info("Ollama track matcher returned HTTP {0} for local '{1}' and candidate '{2}': {3}",
                                 response.StatusCode,
                                 localTrack.FileTrackInfo?.Title ?? localTrack.Path,
                                 candidateTrack.Title,
                                 response.Content);
                    return NoMatch("http_error");
                }

                var responseText = response.Resource.Response;
                if (responseText.IsNullOrWhiteSpace())
                {
                    _logger.Info("Ollama track matcher returned an empty response for local '{0}' and candidate '{1}'. Thinking chars: {2}",
                                 localTrack.FileTrackInfo?.Title ?? localTrack.Path,
                                 candidateTrack.Title,
                                 response.Resource.Thinking?.Length ?? 0);
                    return NoMatch("empty_response");
                }

                var result = Json.Deserialize<OllamaMatchResponse>(StripThinking(responseText));
                result.Confidence = Clamp(result.Confidence);

                _logger.Info("Ollama track matcher result: match={0}, confidence={1:P1}, local '{2}', candidate '{3}'",
                             result.IsMatch,
                             result.Confidence,
                             localTrack.FileTrackInfo?.Title ?? localTrack.Path,
                             candidateTrack.Title);

                if (!result.IsMatch || result.Confidence < MinimumScore)
                {
                    return NoMatch("ollama_rejected");
                }

                return new OllamaTrackMatchResult
                {
                    IsMatch = true,
                    Confidence = result.Confidence,
                    Reason = "ollama_match"
                };
            }
            catch (Exception ex)
            {
                _logger.Info(ex, "Ollama track matcher failed");
                return NoMatch("exception");
            }
        }

        private static OllamaTrackMatchResult NoMatch(string reason)
        {
            return new OllamaTrackMatchResult
            {
                IsMatch = false,
                Confidence = 0.0,
                Reason = reason
            };
        }

        private static string BuildPrompt(LocalTrack localTrack, Track candidateTrack, double currentScore)
        {
            var info = localTrack.FileTrackInfo;
            var localNumbers = info?.TrackNumbers == null ? string.Empty : string.Join(",", info.TrackNumbers);
            var localDuration = info == null ? 0 : (int)Math.Round(info.Duration.TotalSeconds);

            return $@"You help Lidarr decide whether a downloaded audio file is the same track as a MusicBrainz track.
Return only JSON: {{""isMatch"":true|false,""confidence"":0.0-1.0}}.
Be conservative. Extra text like remaster, live, explicit, deluxe, bitrate, source, release group, year, file extension, or bracket tags may appear in the downloaded name and should not prevent a match when the base song title is the same.
Do not match if the actual song title is different, a different mix/version changes identity, or duration/track number strongly disagree.

Current deterministic score: {currentScore.ToString("0.###", CultureInfo.InvariantCulture)}
Downloaded:
- title: {info?.Title}
- artist: {info?.ArtistTitle}
- album: {info?.AlbumTitle}
- disc: {info?.DiscNumber}
- track numbers: {localNumbers}
- duration seconds: {localDuration}
- path: {localTrack.Path}

MusicBrainz candidate:
- title: {candidateTrack.Title}
- track number: {candidateTrack.TrackNumber}
- absolute track number: {candidateTrack.AbsoluteTrackNumber}
- medium: {candidateTrack.MediumNumber}
- duration seconds: {candidateTrack.Duration / 1000}";
        }

        private static string StripThinking(string response)
        {
            return Regex.Replace(response, "<think>.*?</think>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
        }

        private static string GetUrl()
        {
            return (Environment.GetEnvironmentVariable("LIDARR_OLLAMA_URL") ?? DefaultUrl).TrimEnd('/');
        }

        private static string GetModel()
        {
            var model = Environment.GetEnvironmentVariable("LIDARR_OLLAMA_MODEL");
            return model.IsNotNullOrWhiteSpace() ? model : DefaultModel;
        }

        private static string GetKeepAlive()
        {
            var keepAlive = Environment.GetEnvironmentVariable("LIDARR_OLLAMA_KEEP_ALIVE");
            return keepAlive.IsNotNullOrWhiteSpace() ? keepAlive : DefaultKeepAlive;
        }

        private static bool GetBool(string key, bool defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return value.IsNullOrWhiteSpace() ? defaultValue : bool.TryParse(value, out var result) ? result : defaultValue;
        }

        private static double GetDouble(string key, double defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return value.IsNullOrWhiteSpace() ? defaultValue : double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
        }

        private static int GetInt(string key, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return value.IsNullOrWhiteSpace() ? defaultValue : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
        }

        private static double Clamp(double value)
        {
            return Math.Max(0.0, Math.Min(1.0, value));
        }

        private class OllamaGenerateRequest
        {
            public string Model { get; set; }
            public string Prompt { get; set; }
            public bool Stream { get; set; }
            public string Format { get; set; }
            public bool Think { get; set; }
            [JsonProperty("keep_alive")]
            public string KeepAlive { get; set; }
            public OllamaGenerateOptions Options { get; set; }
        }

        private class OllamaGenerateOptions
        {
            public double Temperature { get; set; }
            [JsonProperty("num_predict")]
            public int NumPredict { get; set; }
        }

        private class OllamaGenerateResponse
        {
            public string Response { get; set; }
            public string Thinking { get; set; }
        }

        private class OllamaMatchResponse
        {
            public bool IsMatch { get; set; }
            public double Confidence { get; set; }
        }
    }
}
