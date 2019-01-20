using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TrackImport.Aggregation;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Identification
{
    public interface IIdentificationService
    {
        List<LocalAlbumRelease> Identify(List<LocalTrack> localTracks, Artist artist, Album album, AlbumRelease release, bool newDownload);
    }

    public class IdentificationService : IIdentificationService
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IReleaseService _releaseService;
        private readonly ITrackGroupingService _trackGroupingService;
        private readonly IFingerprintingService _fingerprintingService;
        private readonly IAugmentingService _augmentingService;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public IdentificationService(IArtistService artistService,
                                     IAlbumService albumService,
                                     IReleaseService releaseService,
                                     ITrackGroupingService trackGroupingService,
                                     IFingerprintingService fingerprintingService,
                                     IAugmentingService augmentingService,
                                     IConfigService configService,
                                     Logger logger)
        {
            _artistService = artistService;
            _albumService = albumService;
            _releaseService = releaseService;
            _trackGroupingService = trackGroupingService;
            _fingerprintingService = fingerprintingService;
            _augmentingService = augmentingService;
            _configService = configService;
            _logger = logger;
        }

        private List<IsoCountry> preferredCountries = new List<string> {
            "United Kingdom",
            "United States",
            "Europe",
            "[Worldwide]"
        }.Select(x => IsoCountries.Find(x)).ToList();

        private readonly List<string> VariousArtistNames = new List<string> { "various artists", "various", "va", "unknown" };
        private readonly List<string> VariousArtistIds = new List<string> { "89ad4ac3-39f7-470e-963a-56509c546377" };

        public List<LocalAlbumRelease> Identify(List<LocalTrack> localTracks, Artist artist, Album album, AlbumRelease release, bool newDownload)
        {
            // 1 group localTracks so that we think they represent a single release
            // 2 get candidates given specified artist, album and release
            // 3 find best candidate
            // 4 If best candidate worse than threshold, try fingerprinting

            _logger.Debug("Starting track indentification");
            _logger.Debug("Specified artist {0}, album {1}, release {2}", artist.NullSafe(), album.NullSafe(), release.NullSafe());
            _logger.Trace("Processing files:\n{0}", string.Join("\n", localTracks.Select(x => x.Path)));

            var releases = _trackGroupingService.GroupTracks(localTracks);
            
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
                IdentifyRelease(localRelease, artist, album, release, newDownload);
            }

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
                .OrderByDescending(x => x.Value.Item2.NormalizedDistance())
                .First()
                .Value.Item2.NormalizedDistance() ?? 1.0;
            
            if (localAlbumRelease.Distance.NormalizedDistance() > 0.15 ||
                localAlbumRelease.TrackMapping.LocalExtra.Any() ||
                localAlbumRelease.TrackMapping.MBExtra.Any() ||
                worstTrackMatchDist > 0.40)
            {
                return true;
            }

            return false;
        }

        private void IdentifyRelease(LocalAlbumRelease localAlbumRelease, Artist artist, Album album, AlbumRelease release, bool newDownload)
        {
            bool fingerprinted = false;
            
            var candidateReleases = GetCandidatesFromTags(localAlbumRelease, artist, album, release);
            if (candidateReleases.Count == 0 && FingerprintingAllowed(newDownload))
            {
                _logger.Debug("No candidates found, fingerprinting");
                _fingerprintingService.Lookup(localAlbumRelease.LocalTracks, 0.5);
                fingerprinted = true;
                candidateReleases = GetCandidatesFromFingerprint(localAlbumRelease);
            }

            if (candidateReleases.Count == 0)
            {
                // can't find any candidates even after fingerprinting
                return;
            }
            
            GetBestRelease(localAlbumRelease, candidateReleases);

            // If result isn't great and we haven't fingerprinted, try that
            // Note that this can improve the match even if we try the same candidates
            if (!fingerprinted && FingerprintingAllowed(newDownload) && ShouldFingerprint(localAlbumRelease))
            {
                _fingerprintingService.Lookup(localAlbumRelease.LocalTracks, 0.5);

                // Only include extra possible candidates if neither album nor release are specified
                // Will generally be specified as part of manual import
                if (album == null && release == null)
                {
                    candidateReleases.AddRange(GetCandidatesFromFingerprint(localAlbumRelease).DistinctBy(x => x.Id));
                }

                GetBestRelease(localAlbumRelease, candidateReleases);
            }

            localAlbumRelease.PopulateMatch();
        }

        public List<AlbumRelease> GetCandidatesFromTags(LocalAlbumRelease localAlbumRelease, Artist artist, Album album, AlbumRelease release)
        {
            // Generally artist, album and release are null.  But if they're not then limit candidates appropriately.
            // We've tried to make sure that tracks are all for a single release.
            
            var candidateReleases = new List<AlbumRelease>();

            // if we have a release ID, use that
            var releaseIds = localAlbumRelease.LocalTracks.Select(x => x.FileTrackInfo.ReleaseMBId).Distinct().ToList();
            if (releaseIds.Count == 1 && releaseIds[0].IsNotNullOrWhiteSpace())
            {
                _logger.Debug("Selecting release from consensus ForeignReleaseId [{0}]", releaseIds[0]);
                return _releaseService.GetReleasesByForeignReleaseId(releaseIds);
            }

            if (release != null)
            {
                _logger.Debug("Release {0} [{1} tracks] was forced", release, release.TrackCount);
                candidateReleases = new List<AlbumRelease> { release };
            }
            else if (album != null)
            {
                candidateReleases = GetCandidatesByAlbum(localAlbumRelease, album);
            }
            else if (artist != null)
            {
                candidateReleases = GetCandidatesByArtist(localAlbumRelease, artist);
            }
            else
            {
                candidateReleases = GetCandidates(localAlbumRelease);
            }

            // if we haven't got any candidates then try fingerprinting
            return candidateReleases;
        }

        private List<AlbumRelease> GetCandidatesByAlbum(LocalAlbumRelease localAlbumRelease, Album album)
        {
            // sort candidate releases by closest track count so that we stand a chance of
            // getting a perfect match early on
            return _releaseService.GetReleasesByAlbum(album.Id)
                .OrderBy(x => Math.Abs(localAlbumRelease.TrackCount - x.TrackCount))
                .ToList();
        }

        private List<AlbumRelease> GetCandidatesByArtist(LocalAlbumRelease localAlbumRelease, Artist artist)
        {
            _logger.Trace("Getting candidates for {0}", artist);
            var candidateReleases = new List<AlbumRelease>();
            
            var albumTag = MostCommon(localAlbumRelease.LocalTracks.Select(x => x.FileTrackInfo.AlbumTitle)) ?? "";
            if (albumTag.IsNotNullOrWhiteSpace())
            {
                var possibleAlbums = _albumService.GetCandidates(artist.ArtistMetadataId, albumTag);
                foreach (var album in possibleAlbums)
                {
                    candidateReleases.AddRange(GetCandidatesByAlbum(localAlbumRelease, album));
                }
            }

            return candidateReleases;
        }

        private List<AlbumRelease> GetCandidates(LocalAlbumRelease localAlbumRelease)
        {
            // most general version, nothing has been specified.
            // get all plausible artists, then all plausible albums, then get releases for each of these.

            // check if it looks like VA.
            if (TrackGroupingService.IsVariousArtists(localAlbumRelease.LocalTracks))
            {
                throw new NotImplementedException("Various artists not supported");
            }

            var candidateReleases = new List<AlbumRelease>();
            
            var artistTag = MostCommon(localAlbumRelease.LocalTracks.Select(x => x.FileTrackInfo.ArtistTitle)) ?? "";
            if (artistTag.IsNotNullOrWhiteSpace())
            {
                var possibleArtists = _artistService.GetCandidates(artistTag);
                foreach (var artist in possibleArtists)
                {
                    candidateReleases.AddRange(GetCandidatesByArtist(localAlbumRelease, artist));
                }
            }

            return candidateReleases;
        }

        public List<AlbumRelease> GetCandidatesFromFingerprint(LocalAlbumRelease localAlbumRelease)
        {
            var recordingIds = localAlbumRelease.LocalTracks.Where(x => x.AcoustIdResults != null).SelectMany(x => x.AcoustIdResults).ToList();
            var allReleases = _releaseService.GetReleasesByRecordingIds(recordingIds);

            return allReleases.Select(x => new {
                    Release = x,
                    TrackCount = x.TrackCount,
                    CommonProportion = x.Tracks.Value.Select(y => y.ForeignRecordingId).Intersect(recordingIds).Count() / localAlbumRelease.TrackCount
                })
                .Where(x => x.CommonProportion > 0.6)
                .ToList()
                .OrderBy(x => Math.Abs(x.TrackCount - localAlbumRelease.TrackCount))
                .ThenByDescending(x => x.CommonProportion)
                .Select(x => x.Release)
                .Take(10)
                .ToList();
        }

        private T MostCommon<T>(IEnumerable<T> items)
        {
            return items.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key;
        }

        private void GetBestRelease(LocalAlbumRelease localAlbumRelease, List<AlbumRelease> candidateReleases)
        {
            _logger.Debug("Matching {0} track files against {1} candidates", localAlbumRelease.TrackCount, candidateReleases.Count);
            _logger.Trace("Processing files:\n{0}", string.Join("\n", localAlbumRelease.LocalTracks.Select(x => x.Path)));

            foreach (var release in candidateReleases)
            {
                _logger.Debug("Trying Release {0} [{1}, {2} tracks]", release, release.Title, release.TrackCount);
                var mapping = MapReleaseTracks(localAlbumRelease.LocalTracks, release.Tracks.Value);
                var distance = AlbumReleaseDistance(localAlbumRelease.LocalTracks, release, mapping);
                _logger.Debug("Release {0} [{1} tracks] has distance {2} vs best distance {3}",
                              release, release.TrackCount, distance.NormalizedDistance(), localAlbumRelease.Distance.NormalizedDistance());
                if (distance.NormalizedDistance() < localAlbumRelease.Distance.NormalizedDistance())
                {
                    localAlbumRelease.Distance = distance;
                    localAlbumRelease.AlbumRelease = release;
                    localAlbumRelease.TrackMapping = mapping;
                    if (localAlbumRelease.Distance.NormalizedDistance() == 0.0)
                    {
                        break;
                    }
                }
            }

            _logger.Debug("Best release: {0} Distance {1}", localAlbumRelease.AlbumRelease, localAlbumRelease.Distance.NormalizedDistance());
        }

        public int GetTotalTrackNumber(Track track, List<Track> allTracks)
        {
            return track.AbsoluteTrackNumber + allTracks.Count(t => t.MediumNumber < track.MediumNumber);
        }

        public TrackMapping MapReleaseTracks(List<LocalTrack> localTracks, List<Track> mbTracks)
        {
            var distances = new Distance[localTracks.Count, mbTracks.Count];
            var costs = new double[localTracks.Count, mbTracks.Count];

            for (int row = 0; row < localTracks.Count; row++)
            {
                for (int col = 0; col < mbTracks.Count; col++)
                {
                    distances[row, col] = TrackDistance(localTracks[row], mbTracks[col], GetTotalTrackNumber(mbTracks[col], mbTracks));
                    costs[row, col] = distances[row, col].NormalizedDistance();
                }
            }

            var m = new Munkres(costs);
            m.Run();

            var result = new TrackMapping();
            foreach (var pair in m.Solution)
            {
                result.Mapping.Add(localTracks[pair.Item1], Tuple.Create(mbTracks[pair.Item2], distances[pair.Item1, pair.Item2]));
                _logger.Trace("Mapped {0} to {1}, dist: {2}", localTracks[pair.Item1], mbTracks[pair.Item2], costs[pair.Item1, pair.Item2]);
            }
            result.LocalExtra = localTracks.Except(result.Mapping.Keys).ToList();
            result.MBExtra = mbTracks.Except(result.Mapping.Values.Select(x => x.Item1)).ToList();
            
            return result;
        }

        private bool TrackIndexIncorrect(LocalTrack localTrack, Track mbTrack, int totalTrackNumber)
        {
            return localTrack.FileTrackInfo.TrackNumbers.First() != mbTrack.AbsoluteTrackNumber &&
                localTrack.FileTrackInfo.TrackNumbers.First() != totalTrackNumber;
        }

        public Distance TrackDistance(LocalTrack localTrack, Track mbTrack, int totalTrackNumber, bool includeArtist = false)
        {
            var dist = new Distance();

            var localLength = localTrack.FileTrackInfo.Duration.TotalSeconds;
            var mbLength = mbTrack.Duration / 1000;
            var diff = Math.Abs(localLength - mbLength) - 10;

            if (mbLength > 0)
            {
                dist.AddRatio("track_length", diff, 30);
                _logger.Trace("track_length: {0} vs {1}, diff: {2} grace: 30; {3}",
                              localLength, mbLength, diff, dist.NormalizedDistance());
            }

            // musicbrainz never has 'featuring' in the track title
            // see https://musicbrainz.org/doc/Style/Artist_Credits
            dist.AddString("track_title", localTrack.FileTrackInfo.Title?.CleanTrackTitle() ?? "", mbTrack.Title);
            _logger.Trace("track title: {0} vs {1}; {2}", localTrack.FileTrackInfo.Title, mbTrack.Title, dist.NormalizedDistance());

            if (includeArtist && localTrack.FileTrackInfo.ArtistTitle.IsNotNullOrWhiteSpace()
                && !VariousArtistNames.Any(x => x.Equals(localTrack.FileTrackInfo.ArtistTitle, StringComparison.InvariantCultureIgnoreCase)))
            {
                dist.AddString("track_artist", localTrack.FileTrackInfo.ArtistTitle, mbTrack.ArtistMetadata.Value.Name);
                _logger.Trace("track artist: {0} vs {1}; {2}", localTrack.FileTrackInfo.ArtistTitle, mbTrack.Artist, dist.NormalizedDistance());
            }

            if (localTrack.FileTrackInfo.TrackNumbers.First() > 0 && mbTrack.AbsoluteTrackNumber > 0)
            {
                dist.AddExpr("track_index", () => TrackIndexIncorrect(localTrack, mbTrack, totalTrackNumber));
                _logger.Trace("track_index: {0} vs {1}; {2}", localTrack.FileTrackInfo.TrackNumbers.First(), mbTrack.AbsoluteTrackNumber, dist.NormalizedDistance());
            }

            var recordingId = localTrack.FileTrackInfo.RecordingMBId;
            if (recordingId.IsNotNullOrWhiteSpace())
            {
                dist.AddExpr("recording_id", () => localTrack.FileTrackInfo.RecordingMBId != mbTrack.ForeignRecordingId);
                _logger.Trace("recording_id: {0} vs {1}; {2}", localTrack.FileTrackInfo.RecordingMBId, mbTrack.ForeignRecordingId, dist.NormalizedDistance());
            }

            // for fingerprinted files
            if (localTrack.AcoustIdResults != null)
            {
                dist.AddExpr("recording_id", () => !localTrack.AcoustIdResults.Contains(mbTrack.ForeignRecordingId));
                _logger.Trace("fingerprinting: {0} vs {1}; {2}", string.Join(", ", localTrack.AcoustIdResults), mbTrack.ForeignRecordingId, dist.NormalizedDistance());
            }

            return dist;
        }

        public Distance AlbumReleaseDistance(List<LocalTrack> localTracks, AlbumRelease release, TrackMapping mapping)
        {
            var dist = new Distance();

            if (!VariousArtistIds.Contains(release.Album.Value.ArtistMetadata.Value.ForeignArtistId))
            {
                var artist = MostCommon(localTracks.Select(x => x.FileTrackInfo.ArtistTitle)) ?? "";
                dist.AddString("artist", artist, release.Album.Value.ArtistMetadata.Value.Name);
                _logger.Trace("artist: {0} vs {1}; {2}", artist, release.Album.Value.ArtistMetadata.Value.Name, dist.NormalizedDistance());
            }

            var title = MostCommon(localTracks.Select(x => x.FileTrackInfo.AlbumTitle)) ?? "";
            // Use the album title since the differences in release titles can cause confusion and
            // aren't always correct in the tags
            dist.AddString("album", title, release.Album.Value.Title);
            _logger.Trace("album: {0} vs {1}; {2}", title, release.Title, dist.NormalizedDistance());

            // Number of discs, either as tagged or the max disc number seen
            var discCount = MostCommon(localTracks.Select(x => x.FileTrackInfo.DiscCount));
            discCount = discCount != 0 ? discCount : localTracks.Max(x => x.FileTrackInfo.DiscNumber);
            if (discCount > 0)
            {
                dist.AddNumber("mediums", discCount, release.Media.Count);
                _logger.Trace("mediums: {0} vs {1}; {2}", discCount, release.Media.Count, dist.NormalizedDistance());
            }

            // Year
            var localYear = MostCommon(localTracks.Select(x => x.FileTrackInfo.Year));
            if (release.Album.Value.ReleaseDate.HasValue)
            {
                var mbYear = release.Album.Value.ReleaseDate.Value.Year;
                if (localYear == mbYear)
                {
                    dist.Add("year", 0.0);
                }
                else
                {
                    var diff = Math.Abs(localYear - mbYear);
                    var diff_max = Math.Abs(DateTime.Now.Year - mbYear);
                    dist.AddRatio("year", diff, diff_max);
                }
            }
            else
            {
                // full penalty when there is no year
                dist.Add("year", 1.0);
            }
            _logger.Trace("year: {0} vs {1}; {2}", localYear, release.Album.Value.ReleaseDate?.Year, dist.NormalizedDistance());

            // If we parsed a country from the files use that, otherwise use our preference
            var country = MostCommon(localTracks.Select(x => x.FileTrackInfo.Country));
            if (release.Country.Count > 0)
            {
                if (country != null)
                {
                    dist.AddEquality("country", country.Name, release.Country);
                    _logger.Trace("country: {0} vs {1}; {2}", country, string.Join(", ", release.Country), dist.NormalizedDistance());
                }
                else if (preferredCountries.Count > 0)
                {
                    dist.AddPriority("country", release.Country, preferredCountries.Select(x => x.Name).ToList());
                    _logger.Trace("country priority: {0} vs {1}; {2}", string.Join(", ", preferredCountries.Select(x => x.Name)), string.Join(", ", release.Country), dist.NormalizedDistance());
                }
            }

            var label = MostCommon(localTracks.Select(x => x.FileTrackInfo.Label));
            if (label.IsNotNullOrWhiteSpace())
            {
                dist.AddEquality("label", label, release.Label);
                _logger.Trace("label: {0} vs {1}; {2}", label, string.Join(", ", release.Label), dist.NormalizedDistance());
            }

            var disambig = MostCommon(localTracks.Select(x => x.FileTrackInfo.Disambiguation));
            if (disambig.IsNotNullOrWhiteSpace())
            {
                dist.AddString("albumdisambig", disambig, release.Disambiguation);
                _logger.Trace("albumdisambig: {0} vs {1}; {2}", disambig, release.Disambiguation, dist.NormalizedDistance());
            }
            
            var mbAlbumId = MostCommon(localTracks.Select(x => x.FileTrackInfo.ReleaseMBId));
            if (mbAlbumId.IsNotNullOrWhiteSpace())
            {
                dist.AddEquality("album_id", mbAlbumId, new List<string> { release.ForeignReleaseId });
                _logger.Trace("album_id: {0} vs {1}; {2}", mbAlbumId, release.ForeignReleaseId, dist.NormalizedDistance());
            }

            // tracks
            foreach (var pair in mapping.Mapping)
            {
                dist.Add("tracks", pair.Value.Item2.NormalizedDistance());
            }
            _logger.Trace("after trackMapping: {0}", dist.NormalizedDistance());

            // missing tracks
            foreach (var track in mapping.MBExtra)
            {
                dist.Add("missing_tracks", 1.0);
            }
            _logger.Trace("after missing tracks: {0}", dist.NormalizedDistance());

            // unmatched tracks
            foreach (var track in mapping.LocalExtra)
            {
                dist.Add("unmatched_tracks", 1.0);
            }
            _logger.Trace("after unmatched tracks: {0}", dist.NormalizedDistance());

            return dist;
        }
    }
}
