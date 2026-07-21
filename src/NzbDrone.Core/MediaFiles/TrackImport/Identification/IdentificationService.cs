using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TrackImport.Aggregation;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Identification
{
    public interface IIdentificationService
    {
        List<LocalAlbumRelease> Identify(List<LocalTrack> localTracks, IdentificationOverrides idOverrides, ImportDecisionMakerConfig config);
    }

    public class IdentificationService : IIdentificationService
    {
        private readonly ITrackService _trackService;
        private readonly ITrackGroupingService _trackGroupingService;
        private readonly IFingerprintingService _fingerprintingService;
        private readonly IAudioTagService _audioTagService;
        private readonly IAugmentingService _augmentingService;
        private readonly ICandidateService _candidateService;
        private readonly IOllamaTrackMatcher _ollamaTrackMatcher;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public IdentificationService(ITrackService trackService,
                                     ITrackGroupingService trackGroupingService,
                                     IFingerprintingService fingerprintingService,
                                     IAudioTagService audioTagService,
                                     IAugmentingService augmentingService,
                                     ICandidateService candidateService,
                                     IOllamaTrackMatcher ollamaTrackMatcher,
                                     IConfigService configService,
                                     Logger logger)
        {
            _trackService = trackService;
            _trackGroupingService = trackGroupingService;
            _fingerprintingService = fingerprintingService;
            _audioTagService = audioTagService;
            _augmentingService = augmentingService;
            _candidateService = candidateService;
            _ollamaTrackMatcher = ollamaTrackMatcher;
            _configService = configService;
            _logger = logger;
        }

        private void LogTestCaseOutput(List<LocalTrack> localTracks, Artist artist, Album album, AlbumRelease release, bool newDownload, bool singleRelease)
        {
            var trackData = localTracks.Select(x => new BasicLocalTrack
            {
                Path = x.Path,
                FileTrackInfo = x.FileTrackInfo
            });
            var options = new IdTestCase
            {
                ExpectedMusicBrainzReleaseIds = new List<string> { "expected-id-1", "expected-id-2", "..." },
                LibraryArtists = new List<ArtistTestCase>
                {
                    new ArtistTestCase
                    {
                        Artist = artist?.Metadata.Value.ForeignArtistId ?? "expected-artist-id (dev: don't forget to add metadata profile)",
                        MetadataProfile = artist?.MetadataProfile.Value
                    }
                },
                Artist = artist?.Metadata.Value.ForeignArtistId,
                Album = album?.ForeignAlbumId,
                Release = release?.ForeignReleaseId,
                NewDownload = newDownload,
                SingleRelease = singleRelease,
                Tracks = trackData.ToList()
            };

            var serializerSettings = Json.GetSerializerSettings();
            serializerSettings.Formatting = Formatting.None;

            var output = JsonConvert.SerializeObject(options, serializerSettings);

            _logger.Debug($"*** IdentificationService TestCaseGenerator ***\n{output}");
        }

        public List<LocalAlbumRelease> GetLocalAlbumReleases(List<LocalTrack> localTracks, bool singleRelease)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            List<LocalAlbumRelease> releases = null;
            if (singleRelease)
            {
                releases = new List<LocalAlbumRelease> { new LocalAlbumRelease(localTracks) };
            }
            else
            {
                releases = _trackGroupingService.GroupTracks(localTracks);
            }

            _logger.Debug($"Sorted {localTracks.Count} tracks into {releases.Count} releases in {watch.ElapsedMilliseconds}ms");

            foreach (var localRelease in releases)
            {
                try
                {
                    _augmentingService.Augment(localRelease);
                }
                catch (AugmentingFailedException)
                {
                    _logger.Warn($"Augmentation failed for {localRelease}");
                }
            }

            return releases;
        }

        public List<LocalAlbumRelease> Identify(List<LocalTrack> localTracks, IdentificationOverrides idOverrides, ImportDecisionMakerConfig config)
        {
            // 1 group localTracks so that we think they represent a single release
            // 2 get candidates given specified artist, album and release.  Candidates can include extra files already on disk.
            // 3 find best candidate
            // 4 If best candidate worse than threshold, try fingerprinting
            var watch = System.Diagnostics.Stopwatch.StartNew();

            _logger.Debug("Starting track identification");

            var releases = GetLocalAlbumReleases(localTracks, config.SingleRelease);

            var i = 0;
            foreach (var localRelease in releases)
            {
                i++;
                _logger.ProgressInfo($"Identifying album {i}/{releases.Count}");
                IdentifyRelease(localRelease, idOverrides, config);
            }

            watch.Stop();

            _logger.Debug($"Track identification for {localTracks.Count} tracks took {watch.ElapsedMilliseconds}ms");

            return releases;
        }

        private bool FingerprintingAllowed(bool newDownload)
        {
            if (_configService.AllowFingerprinting == AllowFingerprinting.Never ||
                (_configService.AllowFingerprinting == AllowFingerprinting.NewFiles && !newDownload))
            {
                return false;
            }

            return true;
        }

        private bool ShouldFingerprint(LocalAlbumRelease localAlbumRelease)
        {
            var worstTrackMatchDist = localAlbumRelease.TrackMapping?.Mapping
                .Select(x => x.Value.Item2.NormalizedDistance())
                .DefaultIfEmpty(1.0)
                .Max() ?? 1.0;

            if (localAlbumRelease.Distance.NormalizedDistance() > 0.15 ||
                localAlbumRelease.TrackMapping.LocalExtra.Any() ||
                localAlbumRelease.TrackMapping.MBExtra.Any() ||
                worstTrackMatchDist > 0.40)
            {
                return true;
            }

            return false;
        }

        private List<LocalTrack> ToLocalTrack(IEnumerable<TrackFile> trackfiles, LocalAlbumRelease localRelease)
        {
            var scanned = trackfiles.Join(localRelease.LocalTracks, t => t.Path, l => l.Path, (track, localTrack) => localTrack);
            var toScan = trackfiles.ExceptBy(t => t.Path, scanned, s => s.Path, StringComparer.InvariantCulture);
            var localTracks = scanned.Concat(toScan.Select(x => new LocalTrack
            {
                Path = x.Path,
                Size = x.Size,
                Modified = x.Modified,
                FileTrackInfo = _audioTagService.ReadTags(x.Path),
                ExistingFile = true,
                AdditionalFile = true,
                Quality = x.Quality
            }))
            .ToList();

            localTracks.ForEach(x => _augmentingService.Augment(x, true));

            return localTracks;
        }

        private void IdentifyRelease(LocalAlbumRelease localAlbumRelease, IdentificationOverrides idOverrides, ImportDecisionMakerConfig config)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var fingerprinted = false;

            var candidateReleases = _candidateService.GetDbCandidatesFromTags(localAlbumRelease, idOverrides, config.IncludeExisting);

            if (candidateReleases.Count == 0 && config.AddNewArtists)
            {
                candidateReleases = _candidateService.GetRemoteCandidates(localAlbumRelease);
            }

            if (candidateReleases.Count == 0 && FingerprintingAllowed(config.NewDownload))
            {
                _logger.Debug("No candidates found, fingerprinting");
                _fingerprintingService.Lookup(localAlbumRelease.LocalTracks, 0.5);
                fingerprinted = true;
                candidateReleases = _candidateService.GetDbCandidatesFromFingerprint(localAlbumRelease, idOverrides, config.IncludeExisting);

                if (candidateReleases.Count == 0 && config.AddNewArtists)
                {
                    // Now fingerprints are populated this will return a different answer
                    candidateReleases = _candidateService.GetRemoteCandidates(localAlbumRelease);
                }
            }

            if (candidateReleases.Count == 0)
            {
                // can't find any candidates even after fingerprinting
                // populate the overrides and return
                foreach (var localTrack in localAlbumRelease.LocalTracks)
                {
                    localTrack.Release = idOverrides.AlbumRelease;
                    localTrack.Album = idOverrides.Album;
                    localTrack.Artist = idOverrides.Artist;
                }

                return;
            }

            _logger.Debug($"Got {candidateReleases.Count} candidates for {localAlbumRelease.LocalTracks.Count} tracks in {watch.ElapsedMilliseconds}ms");

            PopulateTracks(candidateReleases);

            // convert all the TrackFiles that represent extra files to List<LocalTrack>
            var allLocalTracks = ToLocalTrack(candidateReleases
                                              .SelectMany(x => x.ExistingTracks)
                                              .DistinctBy(x => x.Path), localAlbumRelease);

            _logger.Debug($"Retrieved {allLocalTracks.Count} possible tracks in {watch.ElapsedMilliseconds}ms");

            GetBestRelease(localAlbumRelease, candidateReleases, allLocalTracks);

            // If result isn't great and we haven't fingerprinted, try that
            // Note that this can improve the match even if we try the same candidates
            if (!fingerprinted && FingerprintingAllowed(config.NewDownload) && ShouldFingerprint(localAlbumRelease))
            {
                _logger.Debug($"Match not good enough, fingerprinting");
                _fingerprintingService.Lookup(localAlbumRelease.LocalTracks, 0.5);

                // Only include extra possible candidates if neither album nor release are specified
                // Will generally be specified as part of manual import
                if (idOverrides?.Album == null && idOverrides?.AlbumRelease == null)
                {
                    var dbCandidates = _candidateService.GetDbCandidatesFromFingerprint(localAlbumRelease, idOverrides, config.IncludeExisting);
                    var remoteCandidates = config.AddNewArtists ? _candidateService.GetRemoteCandidates(localAlbumRelease) : new List<CandidateAlbumRelease>();
                    var extraCandidates = dbCandidates.Concat(remoteCandidates);
                    var newCandidates = extraCandidates.ExceptBy(x => x.AlbumRelease.Id, candidateReleases, y => y.AlbumRelease.Id, EqualityComparer<int>.Default);
                    candidateReleases.AddRange(newCandidates);

                    PopulateTracks(candidateReleases);

                    allLocalTracks.AddRange(ToLocalTrack(newCandidates
                                                         .SelectMany(x => x.ExistingTracks)
                                                         .DistinctBy(x => x.Path)
                                                         .ExceptBy(x => x.Path, allLocalTracks, x => x.Path, PathEqualityComparer.Instance),
                                                         localAlbumRelease));
                }

                // fingerprint all the local files in candidates we might be matching against
                _fingerprintingService.Lookup(allLocalTracks, 0.5);

                GetBestRelease(localAlbumRelease, candidateReleases, allLocalTracks);
            }

            _logger.Debug($"Best release found in {watch.ElapsedMilliseconds}ms");

            localAlbumRelease.PopulateMatch();

            _logger.Debug($"IdentifyRelease done in {watch.ElapsedMilliseconds}ms");
        }

        public void PopulateTracks(List<CandidateAlbumRelease> candidateReleases)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var releasesMissingTracks = candidateReleases.Where(x => !x.AlbumRelease.Tracks.IsLoaded);
            var allTracks = _trackService.GetTracksByReleases(releasesMissingTracks.Select(x => x.AlbumRelease.Id).ToList());

            _logger.Debug($"Retrieved {allTracks.Count} possible tracks in {watch.ElapsedMilliseconds}ms");

            foreach (var release in releasesMissingTracks)
            {
                release.AlbumRelease.Tracks = allTracks.Where(x => x.AlbumReleaseId == release.AlbumRelease.Id).ToList();
            }
        }

        private void GetBestRelease(LocalAlbumRelease localAlbumRelease, List<CandidateAlbumRelease> candidateReleases, List<LocalTrack> extraTracksOnDisk)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var releaseConcurrency = GetPositiveInt("LIDARR_IDENTIFICATION_RELEASE_CONCURRENCY", 1);

            _logger.Debug("Matching {0} track files against {1} candidates with release concurrency {2}",
                          localAlbumRelease.TrackCount,
                          candidateReleases.Count,
                          releaseConcurrency);
            _logger.Trace("Processing files:\n{0}", string.Join("\n", localAlbumRelease.LocalTracks.Select(x => x.Path)));

            var evaluations = new List<CandidateReleaseEvaluation>();
            var evaluationsLock = new object();

            Parallel.ForEach(candidateReleases, new ParallelOptions { MaxDegreeOfParallelism = releaseConcurrency }, candidateRelease =>
            {
                var evaluation = EvaluateCandidateRelease(localAlbumRelease, candidateRelease, extraTracksOnDisk);
                lock (evaluationsLock)
                {
                    evaluations.Add(evaluation);
                }
            });

            var best = evaluations.OrderBy(x => x.NormalizedDistance).FirstOrDefault();
            if (best != null)
            {
                localAlbumRelease.Distance = best.Distance;
                localAlbumRelease.AlbumRelease = best.Release;
                localAlbumRelease.ExistingTracks = best.ExtraTracks;
                localAlbumRelease.TrackMapping = best.Mapping;
            }

            watch.Stop();
            _logger.Debug($"Best release: {localAlbumRelease.AlbumRelease} Distance {localAlbumRelease.Distance.NormalizedDistance()} found in {watch.ElapsedMilliseconds}ms");
        }

        private CandidateReleaseEvaluation EvaluateCandidateRelease(LocalAlbumRelease localAlbumRelease, CandidateAlbumRelease candidateRelease, List<LocalTrack> extraTracksOnDisk)
        {
            var release = candidateRelease.AlbumRelease;
            _logger.Debug("Trying Release {0} [{1}, {2} tracks, {3} existing]", release, release.Title, release.TrackCount, candidateRelease.ExistingTracks.Count);
            var rwatch = System.Diagnostics.Stopwatch.StartNew();

            var extraTrackPaths = candidateRelease.ExistingTracks.Select(x => x.Path).ToHashSet(PathEqualityComparer.Instance);
            var extraTracks = extraTracksOnDisk.Where(x => extraTrackPaths.Contains(x.Path)).ToList();
            var allLocalTracks = localAlbumRelease.LocalTracks.Concat(extraTracks).DistinctBy(x => x.Path).ToList();

            var mapping = MapReleaseTracks(allLocalTracks, release.Tracks.Value);
            var distance = DistanceCalculator.AlbumReleaseDistance(allLocalTracks, release, mapping);
            var currDistance = distance.NormalizedDistance();

            rwatch.Stop();
            _logger.Debug("Release {0} [{1} tracks] has distance {2} [{3}ms]",
                          release,
                          release.TrackCount,
                          currDistance,
                          rwatch.ElapsedMilliseconds);

            return new CandidateReleaseEvaluation
            {
                Release = release,
                ExtraTracks = extraTracks,
                Mapping = mapping,
                Distance = distance,
                NormalizedDistance = currDistance
            };
        }

        public TrackMapping MapReleaseTracks(List<LocalTrack> localTracks, List<Track> mbTracks)
        {
            var distances = new Distance[localTracks.Count, mbTracks.Count];
            var costs = new double[localTracks.Count, mbTracks.Count];

            var trackDistanceConcurrency = GetPositiveInt("LIDARR_IDENTIFICATION_TRACK_DISTANCE_CONCURRENCY", 1);
            Parallel.For(0, mbTracks.Count, new ParallelOptions { MaxDegreeOfParallelism = trackDistanceConcurrency }, col =>
            {
                var totalTrackNumber = DistanceCalculator.GetTotalTrackNumber(mbTracks[col], mbTracks);
                for (var row = 0; row < localTracks.Count; row++)
                {
                    distances[row, col] = DistanceCalculator.TrackDistance(localTracks[row], mbTracks[col], totalTrackNumber, false);
                    costs[row, col] = distances[row, col].NormalizedDistance();
                }
            });

            ApplyOllamaTrackMatches(localTracks, mbTracks, distances, costs);

            var m = new Munkres(costs);
            m.Run();

            var result = new TrackMapping();
            foreach (var pair in m.Solution)
            {
                result.Mapping.Add(localTracks[pair.Item1], Tuple.Create(mbTracks[pair.Item2], distances[pair.Item1, pair.Item2]));
                _logger.Trace("Mapped {0} to {1}, dist: {2}", localTracks[pair.Item1], mbTracks[pair.Item2], costs[pair.Item1, pair.Item2]);
            }

            result.LocalExtra = localTracks.Except(result.Mapping.Keys).ToList();
            _logger.Trace($"Unmapped files:\n{string.Join("\n", result.LocalExtra)}");

            result.MBExtra = mbTracks.Except(result.Mapping.Values.Select(x => x.Item1)).ToList();
            _logger.Trace($"Missing tracks:\n{string.Join("\n", result.MBExtra)}");

            return result;
        }

        private void ApplyOllamaTrackMatches(List<LocalTrack> localTracks, List<Track> mbTracks, Distance[,] distances, double[,] costs)
        {
            if (!_ollamaTrackMatcher.IsEnabled)
            {
                _logger.Info("Skipping Ollama track matching because it is disabled");
                return;
            }

            if (_ollamaTrackMatcher.RequireEqualTrackCount && localTracks.Count != mbTracks.Count)
            {
                _logger.Debug("Skipping Ollama track matching because local track count {0} does not match candidate track count {1}", localTracks.Count, mbTracks.Count);
                return;
            }

            var initialMapping = new Munkres(costs);
            initialMapping.Run();

            var lowConfidencePairs = initialMapping.Solution
                                                   .Select(pair => new
                                                   {
                                                       Row = pair.Item1,
                                                       Col = pair.Item2,
                                                       CurrentScore = 1.0 - costs[pair.Item1, pair.Item2]
                                                   })
                                                   .Where(pair => pair.CurrentScore < _ollamaTrackMatcher.MinimumScore)
                                                   .ToList();

            if (!lowConfidencePairs.Any())
            {
                return;
            }

            var attemptedMatches = 0;
            var improvedMatches = 0;
            var maxConcurrency = _ollamaTrackMatcher.MaxConcurrency;

            _logger.Info("Ollama track matching starting for candidate release: attempted {0}, max concurrency {1}",
                         lowConfidencePairs.Count,
                         maxConcurrency);

            Parallel.ForEach(lowConfidencePairs, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency }, pair =>
            {
                Interlocked.Increment(ref attemptedMatches);

                _logger.Info("Ollama fallback triggered for track score {0:P1} below threshold {1:P1}: {2} -> {3}",
                             pair.CurrentScore,
                             _ollamaTrackMatcher.MinimumScore,
                             localTracks[pair.Row],
                             mbTracks[pair.Col]);

                var match = _ollamaTrackMatcher.Match(localTracks[pair.Row], mbTracks[pair.Col], pair.CurrentScore);
                if (!match.IsMatch)
                {
                    return;
                }

                var boostedScore = BlendScore(pair.CurrentScore, match.Confidence, _ollamaTrackMatcher.ScoreWeight);
                distances[pair.Row, pair.Col].Set("track_title", 1.0 - match.Confidence);
                ApplyScoreTarget(distances[pair.Row, pair.Col], boostedScore);
                costs[pair.Row, pair.Col] = distances[pair.Row, pair.Col].NormalizedDistance();
                Interlocked.Increment(ref improvedMatches);

                _logger.Info("Ollama improved track match score from {0:P1} to {1:P1} using confidence {2:P1} and weight {3:P0}: {4} -> {5}",
                             pair.CurrentScore,
                             1.0 - costs[pair.Row, pair.Col],
                             match.Confidence,
                             _ollamaTrackMatcher.ScoreWeight,
                             localTracks[pair.Row],
                             mbTracks[pair.Col]);
            });

            _logger.Info("Ollama track matching completed for candidate release: attempted {0}, improved {1}, max concurrency {2}",
                         attemptedMatches,
                         improvedMatches,
                         maxConcurrency);
        }

        private static double BlendScore(double currentScore, double llmConfidence, double llmWeight)
        {
            llmWeight = Clamp(llmWeight);
            return Clamp((currentScore * (1.0 - llmWeight)) + (llmConfidence * llmWeight));
        }

        private static void ApplyScoreTarget(Distance distance, double targetScore)
        {
            var targetDistance = 1.0 - Clamp(targetScore);
            var currentRawDistance = distance.RawDistance();
            var currentMaxDistance = distance.MaxDistance();
            const double ollamaMatchWeight = 10.0;

            var ollamaPenalty = ((targetDistance * (currentMaxDistance + ollamaMatchWeight)) - currentRawDistance) / ollamaMatchWeight;
            distance.Set("ollama_match", Clamp(ollamaPenalty));
        }

        private static int GetPositiveInt(string key, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return value.IsNullOrWhiteSpace() || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? defaultValue : Math.Max(1, result);
        }

        private static double Clamp(double value)
        {
            return Math.Max(0.0, Math.Min(1.0, value));
        }

        private class CandidateReleaseEvaluation
        {
            public AlbumRelease Release { get; set; }
            public List<LocalTrack> ExtraTracks { get; set; }
            public TrackMapping Mapping { get; set; }
            public Distance Distance { get; set; }
            public double NormalizedDistance { get; set; }
        }
    }
}
