using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
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
        private readonly IReleaseService _releaseService;
        private readonly ITrackService _trackService;
        private readonly IAlbumCutoffService _albumCutoffService;
        private readonly IQueueService _queueService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly Logger _logger;

        public AlbumSearchService(ISearchForReleases nzbSearchService,
            IAlbumService albumService,
            IReleaseService releaseService,
            ITrackService trackService,
            IAlbumCutoffService albumCutoffService,
            IQueueService queueService,
            IProcessDownloadDecisions processDownloadDecisions,
            Logger logger)
        {
            _releaseSearchService = nzbSearchService;
            _albumService = albumService;
            _releaseService = releaseService;
            _trackService = trackService;
            _albumCutoffService = albumCutoffService;
            _queueService = queueService;
            _processDownloadDecisions = processDownloadDecisions;
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
                }

                allDecisions.AddRange(decisions);
            }

            _logger.ProgressInfo("Track search found {0} candidate albums containing {1} requested recordings.", candidateAlbums.Count, targetRecordingIds.Count);

            return MergeTrackSearchDecisions(allDecisions);
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

                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Artist.Value.Monitored == true);

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

                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Artist.Value.Monitored == true);

                albums = _albumService.AlbumsWithoutFiles(pagingSpec).Records.ToList();
            }

            var queue = _queueService.GetQueue().Where(q => q.Album != null).Select(q => q.Album.Id);
            var missing = albums.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkAlbums(missing, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
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

            pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Artist.Value.Monitored == true);

            var albums = _albumCutoffService.AlbumsWhereCutoffUnmet(pagingSpec).Records.ToList();
            var queue = _queueService.GetQueue().Where(q => q.Album != null).Select(q => q.Album.Id);
            var cutoffUnmet = albums.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkAlbums(cutoffUnmet, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }
    }
}
