using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Music
{
    public class RefreshArtistService : RefreshEntityServiceBase<Artist, Album>,
        IExecute<RefreshArtistCommand>,
        IExecute<BulkRefreshArtistCommand>
    {
        private readonly IProvideArtistInfo _artistInfo;
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IRefreshAlbumService _refreshAlbumService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IMediaFileService _mediaFileService;
        private readonly IHistoryService _historyService;
        private readonly IRootFolderService _rootFolderService;
        private readonly ICheckIfArtistShouldBeRefreshed _checkIfArtistShouldBeRefreshed;
        private readonly IMonitorNewAlbumService _monitorNewAlbumService;
        private readonly IConfigService _configService;
        private readonly IAutoTaggingService _autoTaggingService;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly Logger _logger;

        public RefreshArtistService(IProvideArtistInfo artistInfo,
                                    IArtistService artistService,
                                    IArtistMetadataService artistMetadataService,
                                    IAlbumService albumService,
                                    IRefreshAlbumService refreshAlbumService,
                                    IEventAggregator eventAggregator,
                                    IManageCommandQueue commandQueueManager,
                                    IMediaFileService mediaFileService,
                                    IHistoryService historyService,
                                    IRootFolderService rootFolderService,
                                    ICheckIfArtistShouldBeRefreshed checkIfArtistShouldBeRefreshed,
                                    IMonitorNewAlbumService monitorNewAlbumService,
                                    IConfigService configService,
                                    IAutoTaggingService autoTaggingService,
                                    IImportListExclusionService importListExclusionService,
                                    Logger logger)
        : base(logger, artistMetadataService)
        {
            _artistInfo = artistInfo;
            _artistService = artistService;
            _albumService = albumService;
            _refreshAlbumService = refreshAlbumService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _mediaFileService = mediaFileService;
            _historyService = historyService;
            _rootFolderService = rootFolderService;
            _checkIfArtistShouldBeRefreshed = checkIfArtistShouldBeRefreshed;
            _monitorNewAlbumService = monitorNewAlbumService;
            _configService = configService;
            _autoTaggingService = autoTaggingService;
            _importListExclusionService = importListExclusionService;
            _logger = logger;
        }

        protected override RemoteData GetRemoteData(Artist local, List<Artist> remote)
        {
            var result = new RemoteData();
            try
            {
                result.Entity = _artistInfo.GetArtistInfo(local.Metadata.Value.ForeignArtistId, local.MetadataProfileId);
                result.Metadata = new List<ArtistMetadata> { result.Entity.Metadata.Value };
            }
            catch (ArtistNotFoundException)
            {
                if (local.Metadata.Value.Status != ArtistStatusType.Deleted)
                {
                    local.Metadata.Value.Status = ArtistStatusType.Deleted;
                    _artistService.UpdateArtist(local);
                    _logger.Debug("Artist marked as deleted on MusicBrainz for {0}", local.Name);
                    _eventAggregator.PublishEvent(new ArtistUpdatedEvent(local));
                }

                _logger.Error($"Artist '{local.Name}' (mbid {local.Metadata.Value.ForeignArtistId}) was not found, it may have been removed from MusicBrainz.");
            }

            return result;
        }

        protected override bool ShouldDelete(Artist local)
        {
            return !_mediaFileService.GetFilesByArtist(local.Id).Any();
        }

        protected override void LogProgress(Artist local)
        {
            _logger.ProgressInfo("Updating Info for {0}", local.Name);
        }

        protected override bool IsMerge(Artist local, Artist remote)
        {
            return local.ArtistMetadataId != remote.Metadata.Value.Id;
        }

        protected override UpdateResult UpdateEntity(Artist local, Artist remote)
        {
            var result = UpdateResult.None;

            if (!local.Metadata.Value.Equals(remote.Metadata.Value))
            {
                result = UpdateResult.UpdateTags;
            }

            local.UseMetadataFrom(remote);
            local.Metadata = remote.Metadata;
            local.LastInfoSync = DateTime.UtcNow;

            try
            {
                local.Path = new DirectoryInfo(local.Path).FullName;
                local.Path = local.Path.GetActualCasing();
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Couldn't update artist path for " + local.Path);
            }

            return result;
        }

        protected override UpdateResult MoveEntity(Artist local, Artist remote)
        {
            _logger.Debug($"Updating MusicBrainz id for {local} to {remote}");

            // We are moving from one metadata to another (will already have been poplated)
            local.ArtistMetadataId = remote.Metadata.Value.Id;
            local.Metadata = remote.Metadata.Value;

            // Update list exclusion if one exists
            var importExclusion = _importListExclusionService.FindByForeignId(local.Metadata.Value.ForeignArtistId);

            if (importExclusion != null)
            {
                importExclusion.ForeignId = remote.Metadata.Value.ForeignArtistId;
                _importListExclusionService.Update(importExclusion);
            }

            // Do the standard update
            UpdateEntity(local, remote);

            // We know we need to update tags as artist id has changed
            return UpdateResult.UpdateTags;
        }

        protected override UpdateResult MergeEntity(Artist local, Artist target, Artist remote)
        {
            _logger.Warn($"Artist {local} was replaced with {remote} because the original was a duplicate.");

            // Update list exclusion if one exists
            var importExclusionLocal = _importListExclusionService.FindByForeignId(local.Metadata.Value.ForeignArtistId);

            if (importExclusionLocal != null)
            {
                var importExclusionTarget = _importListExclusionService.FindByForeignId(target.Metadata.Value.ForeignArtistId);
                if (importExclusionTarget == null)
                {
                    importExclusionLocal.ForeignId = remote.Metadata.Value.ForeignArtistId;
                    _importListExclusionService.Update(importExclusionLocal);
                }
            }

            // move any albums over to the new artist and remove the local artist
            var albums = _albumService.GetAlbumsByArtist(local.Id);
            albums.ForEach(x => x.ArtistMetadataId = target.ArtistMetadataId);
            _albumService.UpdateMany(albums);
            _artistService.DeleteArtist(local.Id, false);

            // Update history entries to new id
            var items = _historyService.GetByArtist(local.Id, null);
            items.ForEach(x => x.ArtistId = target.Id);
            _historyService.UpdateMany(items);

            // We know we need to update tags as artist id has changed
            return UpdateResult.UpdateTags;
        }

        protected override Artist GetEntityByForeignId(Artist local)
        {
            return _artistService.FindById(local.ForeignArtistId);
        }

        protected override void SaveEntity(Artist local)
        {
            _artistService.UpdateArtist(local, publishUpdatedEvent: false);
        }

        protected override void DeleteEntity(Artist local, bool deleteFiles)
        {
            _artistService.DeleteArtist(local.Id, true);
        }

        protected override List<Album> GetRemoteChildren(Artist remote)
        {
            var all = remote.Albums.Value.DistinctBy(m => m.ForeignAlbumId).ToList();
            var ids = all.SelectMany(x => x.OldForeignAlbumIds.Concat(new List<string> { x.ForeignAlbumId })).ToList();
            var excluded = _importListExclusionService.FindByForeignId(ids).Select(x => x.ForeignId).ToList();
            return all.Where(x => !excluded.Contains(x.ForeignAlbumId) && !x.OldForeignAlbumIds.Any(y => excluded.Contains(y))).ToList();
        }

        protected override List<Album> GetLocalChildren(Artist entity, List<Album> remoteChildren)
        {
            return _albumService.GetAlbumsForRefresh(entity.ArtistMetadataId,
                                                     remoteChildren.Select(x => x.ForeignAlbumId)
                                                     .Concat(remoteChildren.SelectMany(x => x.OldForeignAlbumIds)).ToList());
        }

        protected override Tuple<Album, List<Album>> GetMatchingExistingChildren(List<Album> existingChildren, Album remote)
        {
            var existingChild = existingChildren.SingleOrDefault(x => x.ForeignAlbumId == remote.ForeignAlbumId);
            var mergeChildren = existingChildren.Where(x => remote.OldForeignAlbumIds.Contains(x.ForeignAlbumId)).ToList();
            return Tuple.Create(existingChild, mergeChildren);
        }

        protected override void PrepareNewChild(Album child, Artist entity)
        {
            child.Artist = entity;
            child.ArtistMetadata = entity.Metadata.Value;
            child.ArtistMetadataId = entity.Metadata.Value.Id;
            child.Added = DateTime.UtcNow;
            child.LastInfoSync = DateTime.MinValue;
            child.ProfileId = entity.QualityProfileId;
            child.Monitored = entity.Monitored;
        }

        protected override void PrepareExistingChild(Album local, Album remote, Artist entity)
        {
            local.Artist = entity;
            local.ArtistMetadata = entity.Metadata.Value;
            local.ArtistMetadataId = entity.Metadata.Value.Id;
        }

        protected override void ProcessChildren(Artist entity, SortedChildren children)
        {
            foreach (var album in children.Added)
            {
                // all existing child albums count as updated as we don't have proper data yet.
                album.Monitored = _monitorNewAlbumService.ShouldMonitorNewAlbum(album, children.Updated, entity.MonitorNewItems);
            }
        }

        protected override void AddChildren(List<Album> children)
        {
            _albumService.InsertMany(children);
        }

        protected override bool RefreshChildren(SortedChildren localChildren, List<Album> remoteChildren, bool forceChildRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            // we always want to end up refreshing the albums since we don't yet have proper data
            Ensure.That(localChildren.UpToDate.Count, () => localChildren.UpToDate.Count).IsLessThanOrEqualTo(0);
            return _refreshAlbumService.RefreshAlbumInfo(localChildren.All, remoteChildren, forceChildRefresh, forceUpdateFileTags, lastUpdate);
        }

        protected override void PublishEntityUpdatedEvent(Artist entity)
        {
            _eventAggregator.PublishEvent(new ArtistUpdatedEvent(entity));
        }

        protected override void PublishRefreshCompleteEvent(Artist entity)
        {
            _eventAggregator.PublishEvent(new ArtistRefreshCompleteEvent(entity));
        }

        protected override void PublishChildrenUpdatedEvent(Artist entity, List<Album> newChildren, List<Album> updateChildren, List<Album> removedChildren)
        {
            _eventAggregator.PublishEvent(new AlbumInfoRefreshedEvent(entity, newChildren, updateChildren, removedChildren));
        }

        private void RescanArtists(List<Artist> artists, bool isNew, CommandTrigger trigger, bool infoUpdated)
        {
            var rescanAfterRefresh = _configService.RescanAfterRefresh;
            var shouldRescan = true;
            var filter = FilterFilesType.Matched;
            var folders = _rootFolderService.All().Select(x => x.Path).ToList();

            if (isNew)
            {
                _logger.Trace("Forcing rescan. Reason: New artist added");
                shouldRescan = true;

                // only rescan artist folders - otherwise it can be super slow for
                // badly organized / partly matched libraries
                folders = artists.Select(x => x.Path).ToList();
            }
            else if (rescanAfterRefresh == RescanAfterRefreshType.Never)
            {
                _logger.Trace("Skipping rescan. Reason: never rescan after refresh");
                shouldRescan = false;
            }
            else if (rescanAfterRefresh == RescanAfterRefreshType.AfterManual && trigger != CommandTrigger.Manual)
            {
                _logger.Trace("Skipping rescan. Reason: not after automatic refreshes");
                shouldRescan = false;
            }
            else if (!infoUpdated && trigger != CommandTrigger.Manual)
            {
                _logger.Trace("Skipping rescan. Reason: no metadata updated after automatic refresh");
                shouldRescan = false;
            }
            else if (!infoUpdated)
            {
                _logger.Trace("No metadata updated, only scanning new files");
                filter = FilterFilesType.Known;
            }

            if (shouldRescan)
            {
                // some metadata has updated so rescan unmatched
                // (but don't add new artists to reduce repeated searches against api)
                _commandQueueManager.Push(new RescanFoldersCommand(folders, filter, false, artists.Select(x => x.Id).ToList()));
            }
        }

        private void RefreshSelectedArtists(List<int> artistIds, bool isNew, CommandTrigger trigger)
        {
            var updated = false;
            var artists = _artistService.GetArtists(artistIds);

            foreach (var artist in artists)
            {
                try
                {
                    updated |= RefreshEntityInfo(artist, null, true, false, null);
                    UpdateTags(artist);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't refresh info for {0}", artist);
                    UpdateTags(artist);
                }
            }

            RescanArtists(artists, isNew, trigger, updated);
        }

        private void UpdateTags(Artist artist)
        {
            _logger.Trace("Updating tags for {0}", artist);

            var tagsAdded = new HashSet<int>();
            var tagsRemoved = new HashSet<int>();
            var changes = _autoTaggingService.GetTagChanges(artist);

            foreach (var tag in changes.TagsToRemove)
            {
                if (artist.Tags.Contains(tag))
                {
                    artist.Tags.Remove(tag);
                    tagsRemoved.Add(tag);
                }
            }

            foreach (var tag in changes.TagsToAdd)
            {
                if (!artist.Tags.Contains(tag))
                {
                    artist.Tags.Add(tag);
                    tagsAdded.Add(tag);
                }
            }

            if (tagsAdded.Any() || tagsRemoved.Any())
            {
                _artistService.UpdateArtist(artist);
                _logger.Debug("Updated tags for '{0}'. Added: {1}, Removed: {2}", artist.Name, tagsAdded.Count, tagsRemoved.Count);
            }
        }

        public void Execute(BulkRefreshArtistCommand message)
        {
            RefreshSelectedArtists(message.ArtistIds, message.AreNewArtists, message.Trigger);
        }

        public void Execute(RefreshArtistCommand message)
        {
            var trigger = message.Trigger;
            var isNew = message.IsNewArtist;

            if (message.ArtistIds.Any())
            {
                RefreshSelectedArtists(message.ArtistIds, isNew, trigger);
            }
            else
            {
                var updated = false;
                var artists = _artistService.GetAllArtists().OrderBy(c => c.Name).ToList();
                var artistIds = artists.Select(x => x.Id).ToList();

                var updatedMusicbrainzArtists = new HashSet<string>();

                if (message.LastExecutionTime.HasValue && message.LastExecutionTime.Value.AddDays(14) > DateTime.UtcNow)
                {
                    updatedMusicbrainzArtists = _artistInfo.GetChangedArtists(message.LastStartTime.Value);
                }

                foreach (var artist in artists)
                {
                    var manualTrigger = message.Trigger == CommandTrigger.Manual;

                    if ((updatedMusicbrainzArtists == null && _checkIfArtistShouldBeRefreshed.ShouldRefresh(artist)) ||
                        (updatedMusicbrainzArtists != null && updatedMusicbrainzArtists.Contains(artist.ForeignArtistId)) ||
                        manualTrigger)
                    {
                        try
                        {
                            updated |= RefreshEntityInfo(artist, null, manualTrigger, false, message.LastStartTime);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Couldn't refresh info for {0}", artist);
                        }

                        UpdateTags(artist);
                    }
                    else
                    {
                        _logger.Info("Skipping refresh of artist: {0}", artist.Name);
                        UpdateTags(artist);
                    }
                }

                RescanArtists(artists, isNew, trigger, updated);
            }
        }
    }
}
