using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using DryIoc.ImTools;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.TrackImport.Aggregation;
using NzbDrone.Core.MediaFiles.TrackImport.Identification;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.TrackImport
{
    public interface IMakeImportDecision
    {
        List<ImportDecision<LocalTrack>> GetImportDecisions(List<IFileInfo> musicFiles, IdentificationOverrides idOverrides, ImportDecisionMakerInfo itemInfo, ImportDecisionMakerConfig config, List<CueSheetInfo> cueSheetInfos = null);
    }

    public class IdentificationOverrides
    {
        public Artist Artist { get; set; }
        public Album Album { get; set; }
        public AlbumRelease AlbumRelease { get; set; }
    }

    public class ImportDecisionMakerInfo
    {
        public DownloadClientItem DownloadClientItem { get; set; }
        public ParsedAlbumInfo ParsedAlbumInfo { get; set; }
    }

    public class ImportDecisionMakerConfig
    {
        public FilterFilesType Filter { get; set; }
        public bool NewDownload { get; set; }
        public bool SingleRelease { get; set; }
        public bool IncludeExisting { get; set; }
        public bool AddNewArtists { get; set; }
    }

    public class ImportDecisionMaker : IMakeImportDecision
    {
        private readonly IEnumerable<IImportDecisionEngineSpecification<LocalTrack>> _trackSpecifications;
        private readonly IEnumerable<IImportDecisionEngineSpecification<LocalAlbumRelease>> _albumSpecifications;
        private readonly IMediaFileService _mediaFileService;
        private readonly IParsingService _parsingService;
        private readonly IAudioTagService _audioTagService;
        private readonly IAugmentingService _augmentingService;
        private readonly IIdentificationService _identificationService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly Logger _logger;

        public ImportDecisionMaker(IEnumerable<IImportDecisionEngineSpecification<LocalTrack>> trackSpecifications,
                                   IEnumerable<IImportDecisionEngineSpecification<LocalAlbumRelease>> albumSpecifications,
                                   IMediaFileService mediaFileService,
                                   IParsingService parsingService,
                                   IAudioTagService audioTagService,
                                   IAugmentingService augmentingService,
                                   IIdentificationService identificationService,
                                   IRootFolderService rootFolderService,
                                   IQualityProfileService qualityProfileService,
                                   Logger logger)
        {
            _trackSpecifications = trackSpecifications;
            _albumSpecifications = albumSpecifications;
            _mediaFileService = mediaFileService;
            _parsingService = parsingService;
            _audioTagService = audioTagService;
            _augmentingService = augmentingService;
            _identificationService = identificationService;
            _rootFolderService = rootFolderService;
            _qualityProfileService = qualityProfileService;
            _logger = logger;
        }

        public Tuple<List<LocalTrack>, List<ImportDecision<LocalTrack>>> GetLocalTracks(List<IFileInfo> musicFiles, DownloadClientItem downloadClientItem, ParsedAlbumInfo folderInfo, FilterFilesType filter)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var files = _mediaFileService.FilterUnchangedFiles(musicFiles, filter);

            var localTracks = new List<LocalTrack>();
            var decisions = new List<ImportDecision<LocalTrack>>();

            _logger.Debug("Analyzing {0}/{1} files.", files.Count, musicFiles.Count);

            if (!files.Any())
            {
                return Tuple.Create(localTracks, decisions);
            }

            ParsedAlbumInfo downloadClientItemInfo = null;

            if (downloadClientItem != null)
            {
                downloadClientItemInfo = Parser.Parser.ParseAlbumTitle(downloadClientItem.Title);
            }

            var i = 1;
            foreach (var file in files)
            {
                _logger.ProgressInfo($"Reading file {i++}/{files.Count}");

                var localTrack = new LocalTrack
                {
                    DownloadClientAlbumInfo = downloadClientItemInfo,
                    FolderAlbumInfo = folderInfo,
                    Path = file.FullName,
                    Size = file.Length,
                    Modified = file.LastWriteTimeUtc,
                    FileTrackInfo = _audioTagService.ReadTags(file.FullName),
                    AdditionalFile = false,
                };

                try
                {
                    // TODO fix otherfiles?
                    _augmentingService.Augment(localTrack, true);
                    localTracks.Add(localTrack);
                }
                catch (AugmentingFailedException)
                {
                    decisions.Add(new ImportDecision<LocalTrack>(localTrack, new Rejection("Unable to parse file")));
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't import file. {0}", localTrack.Path);

                    decisions.Add(new ImportDecision<LocalTrack>(localTrack, new Rejection("Unexpected error processing file")));
                }
            }

            _logger.Debug($"Tags parsed for {files.Count} files in {watch.ElapsedMilliseconds}ms");

            return Tuple.Create(localTracks, decisions);
        }

        public List<ImportDecision<LocalTrack>> GetImportDecisions(List<IFileInfo> musicFiles, IdentificationOverrides idOverrides, ImportDecisionMakerInfo itemInfo, ImportDecisionMakerConfig config, List<CueSheetInfo> cueSheetInfos)
        {
            idOverrides ??= new IdentificationOverrides();
            itemInfo ??= new ImportDecisionMakerInfo();

            var trackData = GetLocalTracks(musicFiles, itemInfo.DownloadClientItem, itemInfo.ParsedAlbumInfo, config.Filter);
            var localTracks = trackData.Item1;
            var decisions = trackData.Item2;

            localTracks.ForEach(x => x.ExistingFile = !config.NewDownload);
            if (cueSheetInfos != null)
            {
                localTracks.ForEach(localTrack =>
                {
                    var cueSheetFindResult = cueSheetInfos.Find(x => x.IsForMediaFile(localTrack.Path));
                    var cueSheet = cueSheetFindResult?.CueSheet;
                    if (cueSheet != null)
                    {
                        localTrack.IsSingleFileRelease = cueSheet.IsSingleFileRelease;
                        localTrack.Artist = idOverrides.Artist;
                        localTrack.Album = idOverrides.Album;
                    }
                });
            }

            var localTracksByAlbums = localTracks.GroupBy(x => x.Album);
            foreach (var localTracksByAlbum in localTracksByAlbums)
            {
                if (!localTracksByAlbum.All(x => x.IsSingleFileRelease == true))
                {
                    continue;
                }

                localTracks.ForEach(x =>
                {
                    if (x.IsSingleFileRelease && localTracksByAlbum.Contains(x))
                    {
                        x.FileTrackInfo.DiscCount = localTracksByAlbum.Count();
                    }
                });
            }

            var releases = _identificationService.Identify(localTracks, idOverrides, config, cueSheetInfos);

            var albums = releases.GroupBy(x => x.AlbumRelease?.Album?.Value.ForeignAlbumId);

            // group releases that are identified as the same album
            foreach (var album in albums)
            {
                var albumDecisions = new List<ImportDecision<LocalAlbumRelease>>();

                foreach (var release in album)
                {
                    // make sure the appropriate quality profile is set for the release artist
                    // in case it's a new artist
                    EnsureData(release);
                    release.NewDownload = config.NewDownload;

                    albumDecisions.Add(GetDecision(release, itemInfo.DownloadClientItem));
                }

                // if multiple album releases accepted, reject all but one with most tracks
                var acceptedReleases = albumDecisions
                    .Where(x => x.Approved)
                    .OrderByDescending(x => x.Item.TrackCount);
                foreach (var decision in acceptedReleases.Skip(1))
                {
                    decision.Reject(new Rejection("Multiple versions of an album not supported"));
                }

                foreach (var releaseDecision in albumDecisions)
                {
                    foreach (var localTrack in releaseDecision.Item.LocalTracks)
                    {
                        if (releaseDecision.Approved)
                        {
                            decisions.AddIfNotNull(GetDecision(localTrack, itemInfo.DownloadClientItem));
                        }
                        else
                        {
                            decisions.Add(new ImportDecision<LocalTrack>(localTrack, releaseDecision.Rejections.ToArray()));
                        }
                    }
                }
            }

            return decisions;
        }

        private void EnsureData(LocalAlbumRelease release)
        {
            if (release.AlbumRelease != null && release.AlbumRelease.Album.Value.Artist.Value.QualityProfileId == 0)
            {
                var rootFolder = _rootFolderService.GetBestRootFolder(release.LocalTracks.First().Path);
                var qualityProfile = _qualityProfileService.Get(rootFolder.DefaultQualityProfileId);

                var artist = release.AlbumRelease.Album.Value.Artist.Value;
                artist.QualityProfileId = qualityProfile.Id;
                artist.QualityProfile = qualityProfile;
            }
        }

        private ImportDecision<LocalAlbumRelease> GetDecision(LocalAlbumRelease localAlbumRelease, DownloadClientItem downloadClientItem)
        {
            ImportDecision<LocalAlbumRelease> decision = null;

            if (localAlbumRelease.AlbumRelease == null)
            {
                decision = new ImportDecision<LocalAlbumRelease>(localAlbumRelease, new Rejection($"Couldn't find similar album for {localAlbumRelease}"));
            }
            else
            {
                var reasons = _albumSpecifications.Select(c => EvaluateSpec(c, localAlbumRelease, downloadClientItem))
                    .Where(c => c != null);

                decision = new ImportDecision<LocalAlbumRelease>(localAlbumRelease, reasons.ToArray());
            }

            if (decision == null)
            {
                _logger.Error("Unable to make a decision on {0}", localAlbumRelease);
            }
            else if (decision.Rejections.Any())
            {
                _logger.Debug("Album rejected for the following reasons: {0}", string.Join(", ", decision.Rejections));
            }
            else
            {
                _logger.Debug("Album accepted");
            }

            return decision;
        }

        private ImportDecision<LocalTrack> GetDecision(LocalTrack localTrack, DownloadClientItem downloadClientItem)
        {
            ImportDecision<LocalTrack> decision = null;

            if (!localTrack.IsSingleFileRelease && localTrack.Tracks.Empty())
            {
                decision = localTrack.Album != null ? new ImportDecision<LocalTrack>(localTrack, new Rejection($"Couldn't parse track from: {localTrack.FileTrackInfo}")) :
                    new ImportDecision<LocalTrack>(localTrack, new Rejection($"Couldn't parse album from: {localTrack.FileTrackInfo}"));
            }
            else
            {
                var reasons = _trackSpecifications.Select(c => EvaluateSpec(c, localTrack, downloadClientItem))
                    .Where(c => c != null);

                decision = new ImportDecision<LocalTrack>(localTrack, reasons.ToArray());
            }

            if (decision == null)
            {
                _logger.Error("Unable to make a decision on {0}", localTrack.Path);
            }
            else if (decision.Rejections.Any())
            {
                _logger.Debug("File rejected for the following reasons: {0}", string.Join(", ", decision.Rejections));
            }
            else
            {
                _logger.Debug("File accepted");
            }

            return decision;
        }

        private Rejection EvaluateSpec<T>(IImportDecisionEngineSpecification<T> spec, T item, DownloadClientItem downloadClientItem)
        {
            try
            {
                var result = spec.IsSatisfiedBy(item, downloadClientItem);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Couldn't evaluate decision on {0}", item);
                return new Rejection($"{spec.GetType().Name}: {e.Message}");
            }

            return null;
        }
    }
}
