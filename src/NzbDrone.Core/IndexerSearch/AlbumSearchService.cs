using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchForTracks
    {
        Task<List<DownloadDecision>> TrackSearch(List<int> trackIds, bool userInvokedSearch, bool interactiveSearch);
    }

    public class AlbumSearchService : IExecute<AlbumSearchCommand>,
                               IExecute<TrackSearchCommand>,
                               IExecute<MissingAlbumSearchCommand>,
                               IExecute<CutoffUnmetAlbumSearchCommand>,
                               ISearchForTracks
    {
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IAlbumService _albumService;
        private readonly IArtistService _artistService;
        private readonly ISearchForNewAlbum _metadataAlbumSearchService;
        private readonly IRefreshAlbumService _refreshAlbumService;
        private readonly IReleaseService _releaseService;
        private readonly ITrackService _trackService;
        private readonly IAlbumCutoffService _albumCutoffService;
        private readonly IQueueService _queueService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly IUpgradableSpecification _upgradableSpecification;
        private readonly Logger _logger;

        public AlbumSearchService(ISearchForReleases nzbSearchService,
            IAlbumService albumService,
            IArtistService artistService,
            ISearchForNewAlbum metadataAlbumSearchService,
            IRefreshAlbumService refreshAlbumService,
            IReleaseService releaseService,
            ITrackService trackService,
            IAlbumCutoffService albumCutoffService,
            IQueueService queueService,
            IProcessDownloadDecisions processDownloadDecisions,
            IUpgradableSpecification upgradableSpecification,
            Logger logger)
        {
            _releaseSearchService = nzbSearchService;
            _albumService = albumService;
            _artistService = artistService;
            _metadataAlbumSearchService = metadataAlbumSearchService;
            _refreshAlbumService = refreshAlbumService;
            _releaseService = releaseService;
            _trackService = trackService;
            _albumCutoffService = albumCutoffService;
            _queueService = queueService;
            _processDownloadDecisions = processDownloadDecisions;
            _upgradableSpecification = upgradableSpecification;
            _logger = logger;
        }

        private async Task SearchForBulkAlbums(List<Album> albums, bool userInvokedSearch)
        {
            _logger.ProgressInfo("Performing missing search for {0} albums", albums.Count);
            var downloadedCount = 0;

            foreach (var album in albums.OrderBy(a => a.LastSearchTime ?? DateTime.MinValue))
            {
                List<DownloadDecision> decisions;

                try
                {
                    decisions = await _releaseSearchService.AlbumSearch(album.Id, false, userInvokedSearch, false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to search for album: [{0}]", album);
                    continue;
                }

                var processed = await _processDownloadDecisions.ProcessDecisions(decisions);

                downloadedCount += processed.Grabbed.Count;
            }

            _logger.ProgressInfo("Completed search for {0} albums. {1} reports downloaded.", albums.Count, downloadedCount);
        }

        private async Task SearchForSongModeTracks(List<Artist> artists, bool cutoffUnmet, bool userInvokedSearch)
        {
            var queue = _queueService.GetQueue();
            var queuedAlbumIds = queue.Where(item => item.Album != null).Select(item => item.Album.Id).ToHashSet();
            var queuedRecordingIds = queue.Where(item => item.RemoteAlbum?.TargetRecordingIds != null)
                                          .SelectMany(item => item.RemoteAlbum.TargetRecordingIds)
                                          .ToHashSet();
            var downloadedCount = 0;
            var searchedTrackCount = 0;

            foreach (var artist in artists.Where(artist => artist.Monitored && artist.SongMode))
            {
                var trackGroups = _trackService.GetTracksByArtist(artist.Id)
                                               .Where(track => track.Monitored && track.ForeignRecordingId.IsNotNullOrWhiteSpace())
                                               .Where(track => track.AlbumRelease.Value.Album.Value.ReleaseDate <= DateTime.UtcNow)
                                               .GroupBy(track => track.ForeignRecordingId);
                var trackIds = new List<int>();

                foreach (var trackGroup in trackGroups)
                {
                    if (queuedRecordingIds.Contains(trackGroup.Key) ||
                        trackGroup.Any(track => queuedAlbumIds.Contains(track.AlbumRelease.Value.AlbumId)))
                    {
                        continue;
                    }

                    var files = trackGroup.Where(track => track.HasFile)
                                          .Select(track => track.TrackFile.Value)
                                          .DistinctBy(file => file.Id)
                                          .ToList();
                    var shouldSearch = cutoffUnmet ?
                        files.Any() && files.All(file => _upgradableSpecification.QualityCutoffNotMet(artist.QualityProfile.Value, file.Quality)) :
                        files.Empty();

                    if (shouldSearch)
                    {
                        trackIds.Add(trackGroup.First().Id);
                    }
                }

                if (!trackIds.Any())
                {
                    continue;
                }

                searchedTrackCount += trackIds.Count;
                var decisions = await TrackSearch(trackIds, userInvokedSearch, false);
                var processed = await _processDownloadDecisions.ProcessDecisions(decisions, true);
                downloadedCount += processed.Grabbed.Count;
            }

            _logger.ProgressInfo("Completed Song Mode search for {0} songs. {1} reports downloaded.", searchedTrackCount, downloadedCount);
        }

        private List<Artist> GetSongModeArtists(int? artistId)
        {
            if (artistId.HasValue)
            {
                return new List<Artist> { _artistService.GetArtist(artistId.Value) };
            }

            return _artistService.GetAllArtists().Where(artist => artist.SongMode).ToList();
        }

        public void Execute(AlbumSearchCommand message)
        {
            foreach (var albumId in message.AlbumIds)
            {
                var decisions = _releaseSearchService.AlbumSearch(albumId, false, message.Trigger == CommandTrigger.Manual, false).GetAwaiter().GetResult();
                var processed = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();

                _logger.ProgressInfo("Album search completed. {0} reports downloaded.", processed.Grabbed.Count);
            }
        }

        public void Execute(TrackSearchCommand message)
        {
            var decisions = TrackSearch(message.TrackIds, message.Trigger == CommandTrigger.Manual, false).GetAwaiter().GetResult();
            var processed = _processDownloadDecisions.ProcessDecisions(decisions, true).GetAwaiter().GetResult();

            _logger.ProgressInfo("Track search completed from {0} candidate reports. {1} reports downloaded.", decisions.Count, processed.Grabbed.Count);
        }

        public async Task<List<DownloadDecision>> TrackSearch(List<int> trackIds, bool userInvokedSearch, bool interactiveSearch)
        {
            var tracks = _trackService.GetTracks(trackIds);
            var targetRecordingIds = tracks.Select(track => track.ForeignRecordingId)
                                           .Where(id => id.IsNotNullOrWhiteSpace())
                                           .Distinct()
                                           .ToList();

            if (!targetRecordingIds.Any())
            {
                _logger.Warn("Track search could not find MusicBrainz recording IDs for the requested tracks");
                return new List<DownloadDecision>();
            }

            DiscoverSongSearchAlbums(tracks, targetRecordingIds);

            var candidateReleases = _releaseService.GetReleasesByRecordingIds(targetRecordingIds) ?? new List<AlbumRelease>();
            var candidateReleaseIds = candidateReleases.Select(release => release.Id).Distinct().ToList();
            var candidateTracks = candidateReleaseIds.Any() ?
                _trackService.GetTracksByReleases(candidateReleaseIds) :
                new List<Track>();
            var albumIdByReleaseId = candidateReleases.ToDictionary(release => release.Id, release => release.AlbumId);
            var targetRecordingIdsByAlbum = new Dictionary<int, HashSet<string>>();

            foreach (var candidateTrack in candidateTracks.Where(track => targetRecordingIds.Contains(track.ForeignRecordingId)))
            {
                if (!albumIdByReleaseId.TryGetValue(candidateTrack.AlbumReleaseId, out var albumId))
                {
                    continue;
                }

                if (!targetRecordingIdsByAlbum.TryGetValue(albumId, out var albumTargetRecordingIds))
                {
                    albumTargetRecordingIds = new HashSet<string>();
                    targetRecordingIdsByAlbum[albumId] = albumTargetRecordingIds;
                }

                albumTargetRecordingIds.Add(candidateTrack.ForeignRecordingId);
            }

            // Always retain the originally selected album as a fallback if release metadata is incomplete.
            foreach (var track in tracks)
            {
                var albumId = track.AlbumRelease.Value.AlbumId;

                if (!targetRecordingIdsByAlbum.TryGetValue(albumId, out var albumTargetRecordingIds))
                {
                    albumTargetRecordingIds = new HashSet<string>();
                    targetRecordingIdsByAlbum[albumId] = albumTargetRecordingIds;
                }

                albumTargetRecordingIds.Add(track.ForeignRecordingId);
            }

            var candidateAlbums = _albumService.GetAlbums(targetRecordingIdsByAlbum.Keys)
                                               .OrderBy(GetTrackSearchPriority)
                                               .ThenBy(album => album.ReleaseDate ?? DateTime.MaxValue)
                                               .ToList();
            var allDecisions = new List<DownloadDecision>();

            foreach (var album in candidateAlbums)
            {
                var albumTargetRecordingIds = targetRecordingIdsByAlbum[album.Id].ToList();
                var decisions = await _releaseSearchService.AlbumSearch(album.Id, false, userInvokedSearch, interactiveSearch);

                foreach (var decision in decisions)
                {
                    decision.RemoteAlbum.TargetRecordingIds = albumTargetRecordingIds;
                    decision.RemoteAlbum.TrackSearchPriority = decision.RemoteAlbum.ParsedAlbumInfo?.Discography == true ?
                        0 :
                        GetTrackSearchPriority(album) + 1;
                }

                allDecisions.AddRange(decisions);
            }

            _logger.ProgressInfo("Track search found {0} candidate albums containing {1} requested recordings.", candidateAlbums.Count, targetRecordingIds.Count);

            return MergeTrackSearchDecisions(allDecisions);
        }

        private void DiscoverSongSearchAlbums(List<Track> tracks, List<string> targetRecordingIds)
        {
            var selectedArtists = tracks.Select(track => track.AlbumRelease.Value.Album.Value.Artist.Value)
                                        .Where(artist => artist != null)
                                        .DistinctBy(artist => artist.Id)
                                        .ToList();

            if (!selectedArtists.Any())
            {
                return;
            }

            var monitoredRecordingIds = tracks.Where(track => track.Monitored)
                                               .Select(track => track.ForeignRecordingId)
                                               .Where(id => id.IsNotNullOrWhiteSpace())
                                               .ToHashSet();

            foreach (var artist in selectedArtists)
            {
                var artistTracks = _trackService.GetTracksByArtist(artist.Id) ?? new List<Track>();
                monitoredRecordingIds.UnionWith(artistTracks.Where(track => track.Monitored)
                                                            .Select(track => track.ForeignRecordingId)
                                                            .Where(id => id.IsNotNullOrWhiteSpace()));
            }

            List<Album> metadataAlbums;

            try
            {
                metadataAlbums = _metadataAlbumSearchService.SearchForNewAlbumByRecordingIds(targetRecordingIds) ?? new List<Album>();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to discover additional albums containing the requested recordings");
                return;
            }

            foreach (var metadataAlbum in metadataAlbums.Where(album => album.Id == 0))
            {
                var metadataArtist = metadataAlbum.Artist?.Value;
                var artist = selectedArtists.FirstOrDefault(selected =>
                    (selected.Id > 0 && selected.Id == metadataArtist?.Id) ||
                    (selected.ForeignArtistId.IsNotNullOrWhiteSpace() && selected.ForeignArtistId == metadataArtist?.ForeignArtistId));

                if (artist == null || _albumService.FindById(metadataAlbum.ForeignAlbumId) != null)
                {
                    continue;
                }

                metadataAlbum.Artist = artist;
                metadataAlbum.ArtistMetadata = artist.Metadata.Value;
                metadataAlbum.ArtistMetadataId = artist.ArtistMetadataId;
                metadataAlbum.ProfileId = artist.QualityProfileId;
                metadataAlbum.Monitored = false;
                metadataAlbum.Added = DateTime.UtcNow;
                metadataAlbum.AddOptions.AddType = AlbumAddType.SongSearch;

                try
                {
                    _logger.Debug("Adding album {0} as a Song Mode search candidate", metadataAlbum);
                    _albumService.AddAlbum(metadataAlbum, false);
                    _refreshAlbumService.RefreshAlbumInfo(metadataAlbum, null, false);

                    var albumReleaseIds = _releaseService.GetReleasesByAlbum(metadataAlbum.Id)
                                                         .Select(release => release.Id)
                                                         .ToList();
                    var albumTracks = albumReleaseIds.Any() ?
                        _trackService.GetTracksByReleases(albumReleaseIds) :
                        new List<Track>();
                    var unselectedTrackIds = albumTracks.Where(track => !monitoredRecordingIds.Contains(track.ForeignRecordingId))
                                                        .Select(track => track.Id)
                                                        .ToList();
                    var selectedTrackIds = albumTracks.Where(track => monitoredRecordingIds.Contains(track.ForeignRecordingId))
                                                      .Select(track => track.Id)
                                                      .ToList();

                    if (unselectedTrackIds.Any())
                    {
                        _trackService.SetMonitored(unselectedTrackIds, false);
                    }

                    if (selectedTrackIds.Any())
                    {
                        _trackService.SetMonitored(selectedTrackIds, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to add album {0} as a Song Mode search candidate", metadataAlbum);
                }
            }
        }

        private static List<DownloadDecision> MergeTrackSearchDecisions(List<DownloadDecision> decisions)
        {
            var merged = decisions.Where(decision => decision.RemoteAlbum.Release == null ||
                                                     decision.RemoteAlbum.Release.Guid.IsNullOrWhiteSpace())
                                  .ToList();
            var decisionsWithGuid = decisions.Where(decision => decision.RemoteAlbum.Release?.Guid.IsNotNullOrWhiteSpace() == true);

            foreach (var group in decisionsWithGuid.GroupBy(decision => decision.RemoteAlbum.Release.Guid))
            {
                var selected = group.OrderBy(decision => decision.Rejections.Count())
                                    .ThenBy(decision => decision.RemoteAlbum.Release.IndexerPriority)
                                    .First();
                selected.RemoteAlbum.TargetRecordingIds = group.SelectMany(decision => decision.RemoteAlbum.TargetRecordingIds)
                                                               .Distinct()
                                                               .ToList();
                selected.RemoteAlbum.TrackSearchPriority = group.Where(decision => decision.RemoteAlbum.TrackSearchPriority.HasValue)
                                                                .Select(decision => decision.RemoteAlbum.TrackSearchPriority)
                                                                .Min();
                merged.Add(selected);
            }

            return merged;
        }

        private static int GetTrackSearchPriority(Album album)
        {
            if (album.SecondaryTypes.Contains(SecondaryAlbumType.Compilation))
            {
                return 0;
            }

            if (string.Equals(album.AlbumType, PrimaryAlbumType.EP.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(album.AlbumType, PrimaryAlbumType.Single.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(album.AlbumType, PrimaryAlbumType.Album.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                return 3;
            }

            return 4;
        }

        public void Execute(MissingAlbumSearchCommand message)
        {
            List<Album> albums;

            if (message.ArtistId.HasValue)
            {
                var artistId = message.ArtistId.Value;

                var pagingSpec = new PagingSpec<Album>
                {
                    Page = 1,
                    PageSize = 100000,
                    SortDirection = SortDirection.Ascending,
                    SortKey = "Id"
                };

                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Artist.Value.Monitored == true && v.Artist.Value.SongMode == false);

                albums = _albumService.AlbumsWithoutFiles(pagingSpec).Records.Where(e => e.ArtistId.Equals(artistId)).ToList();
            }
            else
            {
                var pagingSpec = new PagingSpec<Album>
                {
                    Page = 1,
                    PageSize = 100000,
                    SortDirection = SortDirection.Ascending,
                    SortKey = "Id"
                };

                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Artist.Value.Monitored == true && v.Artist.Value.SongMode == false);

                albums = _albumService.AlbumsWithoutFiles(pagingSpec).Records.ToList();
            }

            var queue = _queueService.GetQueue().Where(q => q.Album != null).Select(q => q.Album.Id);
            var missing = albums.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkAlbums(missing, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
            SearchForSongModeTracks(GetSongModeArtists(message.ArtistId), false, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }

        public void Execute(CutoffUnmetAlbumSearchCommand message)
        {
            var pagingSpec = new PagingSpec<Album>
            {
                Page = 1,
                PageSize = 100000,
                SortDirection = SortDirection.Ascending,
                SortKey = "Id"
            };

            pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Artist.Value.Monitored == true && v.Artist.Value.SongMode == false);

            var albums = _albumCutoffService.AlbumsWhereCutoffUnmet(pagingSpec).Records
                                             .Where(album => !message.ArtistId.HasValue || album.ArtistId == message.ArtistId.Value)
                                             .ToList();
            var queue = _queueService.GetQueue().Where(q => q.Album != null).Select(q => q.Album.Id);
            var cutoffUnmet = albums.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkAlbums(cutoffUnmet, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
            SearchForSongModeTracks(GetSongModeArtists(message.ArtistId), true, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }
    }
}
