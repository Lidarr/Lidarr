using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.ImportLists.Exclusions;
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
            _logger.ProgressInfo("Starting Import List Sync");

            var rssReleases = _listFetcherAndParser.Fetch();

            var reports = rssReleases.ToList();

            return ProcessReports(reports);
        }

        private List<Album> SyncList(ImportListDefinition definition)
        {
            _logger.ProgressInfo(string.Format("Starting Import List Refresh for List {0}", definition.Name));

            var rssReleases = _listFetcherAndParser.FetchSingleList(definition);

            var reports = rssReleases.ToList();

            return ProcessReports(reports);
        }

        private List<Album> ProcessReports(List<ImportListItemInfo> reports)
        {
            var processed = new List<Album>();
            var artistsToAdd = new List<Artist>();
            var albumsToAdd = new List<Album>();

            _logger.ProgressInfo("Processing {0} list items", reports.Count);

            var reportNumber = 1;

            var listExclusions = _importListExclusionService.All();

            foreach (var report in reports)
            {
                _logger.ProgressTrace("Processing list item {0}/{1}", reportNumber, reports.Count);

                reportNumber++;

                var importList = _importListFactory.Get(report.ImportListId);

                if (report.Album.IsNotNullOrWhiteSpace() || report.AlbumMusicBrainzId.IsNotNullOrWhiteSpace())
                {
                    if (report.AlbumMusicBrainzId.IsNullOrWhiteSpace() || report.ArtistMusicBrainzId.IsNullOrWhiteSpace())
                    {
                        MapAlbumReport(report);
                    }

                    ProcessAlbumReport(importList, report, listExclusions, albumsToAdd, artistsToAdd);
                }
                else if (report.Artist.IsNotNullOrWhiteSpace() || report.ArtistMusicBrainzId.IsNotNullOrWhiteSpace())
                {
                    if (report.ArtistMusicBrainzId.IsNullOrWhiteSpace())
                    {
                        MapArtistReport(report);
                    }

                    ProcessArtistReport(importList, report, listExclusions, artistsToAdd);
                }
            }

            var addedArtists = _addArtistService.AddArtists(artistsToAdd, false, true);
            var addedAlbums = _addAlbumService.AddAlbums(albumsToAdd, false, true);

            var message = string.Format($"Import List Sync Completed. Items found: {reports.Count}, Artists added: {addedArtists.Count}, Albums added: {addedAlbums.Count}");

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
            var mappedAlbum = _albumSearchService.SearchForNewAlbum(albumQuery, report.Artist)
                .FirstOrDefault();

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

        private void ProcessAlbumReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Album> albumsToAdd, List<Artist> artistsToAdd)
        {
            if (report.AlbumMusicBrainzId == null)
            {
                return;
            }

            // Check to see if album in DB
            var existingAlbum = _albumService.FindById(report.AlbumMusicBrainzId);

            // Check to see if album excluded
            var excludedAlbum = listExclusions.SingleOrDefault(s => s.ForeignId == report.AlbumMusicBrainzId);

            // Check to see if artist excluded
            var excludedArtist = listExclusions.SingleOrDefault(s => s.ForeignId == report.ArtistMusicBrainzId);

            if (excludedAlbum != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.AlbumMusicBrainzId, report.Album);
                return;
            }

            if (excludedArtist != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion for parent artist", report.AlbumMusicBrainzId, report.Album);
                return;
            }

            if (existingAlbum != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Album Exists in DB.  Ensuring Album and Artist monitored.", report.AlbumMusicBrainzId, report.Album);

                if (importList.ShouldMonitorExisting && importList.ShouldMonitor != ImportListMonitorType.None)
                {
                    if (!existingAlbum.Monitored)
                    {
                        _albumService.SetAlbumMonitored(existingAlbum.Id, true);
                    }

                    var existingArtist = existingAlbum.Artist.Value;
                    if (importList.ShouldMonitor == ImportListMonitorType.EntireArtist)
                    {
                        _albumService.SetMonitored(existingArtist.Albums.Value.Select(x => x.Id), true);
                    }

                    if (!existingArtist.Monitored)
                    {
                        existingArtist.Monitored = true;
                        _artistService.UpdateArtist(existingArtist);
                    }
                }

                return;
            }

            // Append Album if not already in DB or already on add list
            if (albumsToAdd.All(s => s.ForeignAlbumId != report.AlbumMusicBrainzId))
            {
                var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

                var toAddArtist = new Artist
                {
                    Monitored = monitored,
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

                if (report.ArtistMusicBrainzId != null && report.Artist != null)
                {
                    toAddArtist = ProcessArtistReport(importList, report, listExclusions, artistsToAdd);
                }

                var toAdd = new Album
                {
                    ForeignAlbumId = report.AlbumMusicBrainzId,
                    Monitored = monitored,
                    AnyReleaseOk = true,
                    Artist = toAddArtist,
                    AddOptions = new AddAlbumOptions
                    {
                        SearchForNewAlbum = importList.ShouldSearch
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

        private void MapArtistReport(ImportListItemInfo report)
        {
            var mappedArtist = _artistSearchService.SearchForNewArtist(report.Artist)
                .FirstOrDefault();
            report.ArtistMusicBrainzId = mappedArtist?.Metadata.Value?.ForeignArtistId;
            report.Artist = mappedArtist?.Metadata.Value?.Name;
        }

        private Artist ProcessArtistReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Artist> artistsToAdd)
        {
            if (report.ArtistMusicBrainzId == null)
            {
                return null;
            }

            // Check to see if artist in DB
            var existingArtist = _artistService.FindById(report.ArtistMusicBrainzId);

            // Check to see if artist excluded
            var excludedArtist = listExclusions.Where(s => s.ForeignId == report.ArtistMusicBrainzId).SingleOrDefault();

            // Check to see if artist in import
            var existingImportArtist = artistsToAdd.Find(i => i.ForeignArtistId == report.ArtistMusicBrainzId);

            if (excludedArtist != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.ArtistMusicBrainzId, report.Artist);
                return null;
            }

            if (existingArtist != null)
            {
                _logger.Debug("{0} [{1}] Rejected, artist exists in DB.  Ensuring artist monitored", report.ArtistMusicBrainzId, report.Artist);

                if (importList.ShouldMonitorExisting && !existingArtist.Monitored)
                {
                    existingArtist.Monitored = true;
                    _artistService.UpdateArtist(existingArtist);
                }

                return existingArtist;
            }

            if (existingImportArtist != null)
            {
                _logger.Debug("{0} [{1}] Rejected, artist exists in Import.", report.ArtistMusicBrainzId, report.Artist);

                return existingImportArtist;
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

        public void Execute(ImportListSyncCommand message)
        {
            List<Album> processed;

            if (message.DefinitionId.HasValue)
            {
                processed = SyncList(_importListFactory.Get(message.DefinitionId.Value));
            }
            else
            {
                processed = SyncAll();
            }

            _eventAggregator.PublishEvent(new ImportListSyncCompleteEvent(processed));
        }
    }
}
