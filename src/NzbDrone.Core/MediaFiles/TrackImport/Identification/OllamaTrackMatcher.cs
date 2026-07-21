using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        double ScoreWeight { get; }
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
        private const double DefaultScoreWeight = 1.0;
        private const int DefaultTimeoutSeconds = 10;
        private const int DefaultNumPredict = 128;
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
        public double ScoreWeight => Clamp(GetDouble("LIDARR_OLLAMA_SCORE_WEIGHT", DefaultScoreWeight));

        public OllamaTrackMatchResult Match(LocalTrack localTrack, Track candidateTrack, double currentScore)
        {
            if (!IsEnabled)
            {
                _logger.Info("Ollama track matcher is disabled by LIDARR_OLLAMA_MATCHING_ENABLED");
                return NoMatch("disabled");
            }

            try
            {
                var localTitle = localTrack.FileTrackInfo?.Title ?? localTrack.Path;
                var candidateTitle = candidateTrack.Title;

                if (IsNormalizedExactTitleMatch(localTitle, candidateTitle))
                {
                    _logger.Info("Ollama track matcher accepted normalized exact title match before LLM call: local '{0}', candidate '{1}'",
                                 localTitle,
                                 candidateTitle);
                    return new OllamaTrackMatchResult
                    {
                        IsMatch = true,
                        Confidence = 1.0,
                        Reason = "normalized_title_match"
                    };
                }

                var model = GetModel();
                var url = GetUrl();

                _logger.Info("Ollama track matcher checking low-confidence track match with {0} at {1}: score {2:P1}, local '{3}', candidate '{4}'",
                             model,
                             url,
                             currentScore,
                             localTitle,
                             candidateTitle);

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
                    Format = BuildResponseFormat(),
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
                                 localTitle,
                                 candidateTitle,
                                 response.Content);
                    return NoMatch("http_error");
                }

                var responseText = response.Resource.Response;
                if (responseText.IsNullOrWhiteSpace())
                {
                    _logger.Info("Ollama track matcher returned an empty response for local '{0}' and candidate '{1}'. Thinking chars: {2}",
                                 localTitle,
                                 candidateTitle,
                                 response.Resource.Thinking?.Length ?? 0);
                    return NoMatch("empty_response");
                }

                var normalizedResponse = StripThinking(responseText);
                if (!TryParseMatchResponse(normalizedResponse, out var result, out var parseError))
                {
                    _logger.Info("Ollama track matcher returned an invalid JSON response for local '{0}' and candidate '{1}': {2}. Response: {3}",
                                 localTitle,
                                 candidateTitle,
                                 parseError,
                                 TruncateForLog(normalizedResponse));
                    return NoMatch("invalid_json");
                }

                result.Confidence = Clamp(result.Confidence);

                _logger.Info("Ollama track matcher result: match={0}, confidence={1:P1}, local '{2}', candidate '{3}'",
                             result.IsMatch,
                             result.Confidence,
                             localTitle,
                             candidateTitle);

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
Return only JSON matching the provided schema: {{""isMatch"":true|false,""confidence"":0.0-1.0}}.
Do not include input fields, explanations, markdown, or extra keys.
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

        private static bool IsNormalizedExactTitleMatch(string localTitle, string candidateTitle)
        {
            var normalizedLocal = NormalizeTitleForExactMatch(localTitle);
            var normalizedCandidate = NormalizeTitleForExactMatch(candidateTitle);

            return normalizedLocal.IsNotNullOrWhiteSpace() && normalizedLocal == normalizedCandidate;
        }

        private static string NormalizeTitleForExactMatch(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var normalized = title.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }

        private static JObject BuildResponseFormat()
        {
            return JObject.Parse(@"{
  ""type"": ""object"",
  ""properties"": {
    ""isMatch"": { ""type"": ""boolean"" },
    ""confidence"": { ""type"": ""number"", ""minimum"": 0, ""maximum"": 1 }
  },
  ""required"": [""isMatch"", ""confidence""],
  ""additionalProperties"": false
}");
        }

        private static bool TryParseMatchResponse(string response, out OllamaMatchResponse result, out string error)
        {
            result = null;
            error = null;

            var json = ExtractJsonObject(response);
            if (json.IsNullOrWhiteSpace())
            {
                error = "no JSON object found";
                return false;
            }

            try
            {
                var token = JObject.Parse(json);
                var isMatchToken = token.GetValue("isMatch", StringComparison.OrdinalIgnoreCase);
                var confidenceToken = token.GetValue("confidence", StringComparison.OrdinalIgnoreCase);

                if (isMatchToken == null || confidenceToken == null)
                {
                    error = "missing isMatch or confidence";
                    return false;
                }

                result = new OllamaMatchResponse
                {
                    IsMatch = isMatchToken.Value<bool>(),
                    Confidence = confidenceToken.Value<double>()
                };

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string ExtractJsonObject(string response)
        {
            if (response.IsNullOrWhiteSpace())
            {
                return null;
            }

            var start = response.IndexOf('{');
            if (start < 0)
            {
                return null;
            }

            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var i = start; i < response.Length; i++)
            {
                var current = response[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (current == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                {
                    continue;
                }

                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return response.Substring(start, i - start + 1);
                    }
                }
            }

            return response.Substring(start);
        }

        private static string TruncateForLog(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            const int maxLength = 500;
            value = value.Replace(Environment.NewLine, " ").Trim();
            return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "...");
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
            public object Format { get; set; }
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
