using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListSyncService : IExecute<ImportListSyncCommand>
    {
        private readonly IImportListFactory _importListFactory;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly IFetchAndParseImportList _listFetcherAndParser;
        private readonly ISearchForNewAlbum _albumSearchService;
        private readonly ISearchForNewArtist _artistSearchService;
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IAddArtistService _addArtistService;
        private readonly IAddAlbumService _addAlbumService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportListSyncService(IImportListFactory importListFactory,
                                     IImportListExclusionService importListExclusionService,
                                     IFetchAndParseImportList listFetcherAndParser,
                                     ISearchForNewAlbum albumSearchService,
                                     ISearchForNewArtist artistSearchService,
                                     IArtistService artistService,
                                     IAlbumService albumService,
                                     IAddArtistService addArtistService,
                                     IAddAlbumService addAlbumService,
                                     IEventAggregator eventAggregator,
                                     IManageCommandQueue commandQueueManager,
                                     Logger logger)
        {
            _importListFactory = importListFactory;
            _importListExclusionService = importListExclusionService;
            _listFetcherAndParser = listFetcherAndParser;
            _albumSearchService = albumSearchService;
            _artistSearchService = artistSearchService;
            _artistService = artistService;
            _albumService = albumService;
            _addArtistService = addArtistService;
            _addAlbumService = addAlbumService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        private List<Album> SyncAll()
        {
            if (_importListFactory.AutomaticAddEnabled().Empty())
            {
                _logger.Debug("No import lists with automatic add enabled");

                return new List<Album>();
            }

            _logger.ProgressInfo("Starting Import List Sync");

            var listItems = _listFetcherAndParser.Fetch().ToList();

            return ProcessListItems(listItems);
        }

        private List<Album> SyncList(ImportListDefinition definition)
        {
            _logger.ProgressInfo($"Starting Import List Refresh for List {definition.Name}");

            var listItems = _listFetcherAndParser.FetchSingleList(definition).ToList();

            return ProcessListItems(listItems);
        }

        private List<Album> ProcessListItems(List<ImportListItemInfo> items)
        {
            var processed = new List<Album>();
            var artistsToAdd = new List<Artist>();
            var albumsToAdd = new List<Album>();

            if (items.Count == 0)
            {
                _logger.ProgressInfo("No list items to process");

                return new List<Album>();
            }

            _logger.ProgressInfo("Processing {0} list items", items.Count);

            var reportNumber = 1;

            var listExclusions = _importListExclusionService.All().ToDictionary(x => x.ForeignId);
            var importLists = _importListFactory.All();

            foreach (var item in items)
            {
                _logger.ProgressTrace("Processing list item {0}/{1}", reportNumber++, items.Count);

                var importList = importLists.Single(x => x.Id == item.ImportListId);

                if (item.Album.IsNotNullOrWhiteSpace() || item.AlbumMusicBrainzId.IsNotNullOrWhiteSpace())
                {
                    if (item.AlbumMusicBrainzId.IsNullOrWhiteSpace() || item.ArtistMusicBrainzId.IsNullOrWhiteSpace())
                    {
                        MapAlbumReport(item);
                    }

                    ProcessAlbumReport(importList, item, listExclusions, albumsToAdd, artistsToAdd);
                }
                else if (item.Artist.IsNotNullOrWhiteSpace() || item.ArtistMusicBrainzId.IsNotNullOrWhiteSpace())
                {
                    if (item.ArtistMusicBrainzId.IsNullOrWhiteSpace())
                    {
                        MapArtistReport(item);
                    }

                    ProcessArtistReport(importList, item, listExclusions, artistsToAdd);
                }
            }

            var addedArtists = _addArtistService.AddArtists(artistsToAdd, false, true);
            var addedAlbums = _addAlbumService.AddAlbums(albumsToAdd, false, true);

            var message = string.Format($"Import List Sync Completed. Items found: {items.Count}, Artists added: {addedArtists.Count}, Albums added: {addedAlbums.Count}");

            _logger.ProgressInfo(message);

            var toRefresh = addedArtists.Select(x => x.Id).Concat(addedAlbums.Select(x => x.Artist.Value.Id)).Distinct().ToList();
            if (toRefresh.Any())
            {
                _commandQueueManager.Push(new BulkRefreshArtistCommand(toRefresh, true));
            }

            return processed;
        }

        private void MapAlbumReport(ImportListItemInfo report)
        {
            var albumQuery = report.AlbumMusicBrainzId.IsNotNullOrWhiteSpace() ? $"lidarr:{report.AlbumMusicBrainzId}" : report.Album;
            var mappedAlbum = _albumSearchService.SearchForNewAlbum(albumQuery, report.Artist).FirstOrDefault();

            // Break if we are looking for an album and cant find it. This will avoid us from adding the artist and possibly getting it wrong.
            if (mappedAlbum == null)
            {
                return;
            }

            report.AlbumMusicBrainzId = mappedAlbum.ForeignAlbumId;
            report.Album = mappedAlbum.Title;
            report.Artist ??= mappedAlbum.ArtistMetadata?.Value?.Name;
            report.ArtistMusicBrainzId ??= mappedAlbum.ArtistMetadata?.Value?.ForeignArtistId;
        }

        private void ProcessAlbumReport(ImportListDefinition importList, ImportListItemInfo report, Dictionary<string, ImportListExclusion> listExclusions, List<Album> albumsToAdd, List<Artist> artistsToAdd)
        {
            if (report.AlbumMusicBrainzId.IsNullOrWhiteSpace() || report.ArtistMusicBrainzId.IsNullOrWhiteSpace())
            {
                return;
            }

            // Check to see if album excluded
            if (listExclusions.ContainsKey(report.AlbumMusicBrainzId))
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.AlbumMusicBrainzId, report.Album);
                return;
            }

            // Check to see if artist excluded
            if (listExclusions.ContainsKey(report.ArtistMusicBrainzId))
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion for parent artist", report.AlbumMusicBrainzId, report.Album);
                return;
            }

            // Check to see if album in DB
            var existingAlbum = _albumService.FindById(report.AlbumMusicBrainzId);
            if (existingAlbum != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Album Exists in DB.  Ensuring Album and Artist monitored.", report.AlbumMusicBrainzId, report.Album);

                ProcessAlbumReportForExistingAlbum(importList, existingAlbum);
                return;
            }

            // Append Album if not already in DB or already on add list
            if (albumsToAdd.All(s => s.ForeignAlbumId != report.AlbumMusicBrainzId))
            {
                var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

                var toAddArtist = ProcessArtistReport(importList, report, listExclusions, artistsToAdd);

                var toAdd = new Album
                {
                    ForeignAlbumId = report.AlbumMusicBrainzId,
                    Monitored = monitored,
                    AnyReleaseOk = true,
                    Artist = toAddArtist,
                    AddOptions = new AddAlbumOptions
                    {
                        SearchForNewAlbum = importList.ShouldSearch && toAddArtist.Id > 0
                    }
                };

                if (importList.ShouldMonitor == ImportListMonitorType.SpecificAlbum && toAddArtist.AddOptions != null)
                {
                    Debug.Assert(toAddArtist.Id == 0, "new artist added but ID is not 0");
                    toAddArtist.AddOptions.AlbumsToMonitor.Add(toAdd.ForeignAlbumId);
                }

                albumsToAdd.Add(toAdd);
            }
        }

        private void ProcessAlbumReportForExistingAlbum(ImportListDefinition importList, Album existingAlbum)
        {
            if (importList.ShouldMonitorExisting && importList.ShouldMonitor != ImportListMonitorType.None)
            {
                Command searchCommand = null;
                var existingArtist = existingAlbum.Artist.Value;

                if (!existingArtist.Monitored || !existingAlbum.Monitored)
                {
                    searchCommand = importList.ShouldMonitor == ImportListMonitorType.EntireArtist ? new MissingAlbumSearchCommand(existingArtist.Id) : new AlbumSearchCommand(new List<int> { existingAlbum.Id });
                }

                if (!existingAlbum.Monitored)
                {
                    _albumService.SetAlbumMonitored(existingAlbum.Id, true);
                }

                if (!existingArtist.Monitored)
                {
                    existingArtist.Monitored = true;
                    _artistService.UpdateArtist(existingArtist);
                }

                // Make sure all artist albums are monitored if required
                if (importList.ShouldMonitor == ImportListMonitorType.EntireArtist && existingArtist.Albums.Value.Any(x => !x.Monitored))
                {
                    _albumService.SetMonitored(existingArtist.Albums.Value.Select(x => x.Id), true);
                    searchCommand = new MissingAlbumSearchCommand(existingArtist.Id);
                }

                if (importList.ShouldSearch && searchCommand != null)
                {
                    _commandQueueManager.Push(searchCommand);
                }
            }
        }

        private void MapArtistReport(ImportListItemInfo report)
        {
            var mappedArtist = _artistSearchService.SearchForNewArtist(report.Artist).FirstOrDefault();
            report.ArtistMusicBrainzId = mappedArtist?.Metadata.Value?.ForeignArtistId;
            report.Artist = mappedArtist?.Metadata.Value?.Name;
        }

        private Artist ProcessArtistReport(ImportListDefinition importList, ImportListItemInfo report, Dictionary<string, ImportListExclusion> listExclusions, List<Artist> artistsToAdd)
        {
            if (report.ArtistMusicBrainzId.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (listExclusions.ContainsKey(report.ArtistMusicBrainzId))
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.ArtistMusicBrainzId, report.Artist);
                return null;
            }

            // Check to see if artist in import
            var existingImportArtist = artistsToAdd.Find(i => i.ForeignArtistId == report.ArtistMusicBrainzId);
            if (existingImportArtist != null)
            {
                _logger.Debug("{0} [{1}] Rejected, artist exists in Import.", report.ArtistMusicBrainzId, report.Artist);

                return existingImportArtist;
            }

            // Check to see if artist in DB
            var existingArtist = _artistService.FindById(report.ArtistMusicBrainzId);
            if (existingArtist != null)
            {
                _logger.Debug("{0} [{1}] Rejected, artist exists in DB.  Ensuring artist monitored", report.ArtistMusicBrainzId, report.Artist);

                ProcessArtistReportForExistingArtist(importList, existingArtist);
                return existingArtist;
            }

            var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

            var toAdd = new Artist
            {
                Metadata = new ArtistMetadata
                {
                    ForeignArtistId = report.ArtistMusicBrainzId,
                    Name = report.Artist
                },
                Monitored = monitored,
                MonitorNewItems = importList.MonitorNewItems,
                RootFolderPath = importList.RootFolderPath,
                QualityProfileId = importList.ProfileId,
                MetadataProfileId = importList.MetadataProfileId,
                Tags = importList.Tags,
                AddOptions = new AddArtistOptions
                {
                    SearchForMissingAlbums = importList.ShouldSearch,
                    Monitored = monitored,
                    Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                }
            };

            artistsToAdd.Add(toAdd);

            return toAdd;
        }

        private void ProcessArtistReportForExistingArtist(ImportListDefinition importList, Artist existingArtist)
        {
            if (importList.ShouldMonitorExisting && !existingArtist.Monitored)
            {
                existingArtist.Monitored = true;
                _artistService.UpdateArtist(existingArtist);

                if (importList.ShouldSearch)
                {
                    _commandQueueManager.Push(new MissingAlbumSearchCommand(existingArtist.Id));
                }
            }
        }

        public void Execute(ImportListSyncCommand message)
        {
            var processed = message.DefinitionId.HasValue ? SyncList(_importListFactory.Get(message.DefinitionId.Value)) : SyncAll();

            _eventAggregator.PublishEvent(new ImportListSyncCompleteEvent(processed));
        }
    }
}
