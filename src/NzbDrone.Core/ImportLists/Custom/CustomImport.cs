using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Custom
{
    public class CustomImport : ImportListBase<CustomSettings>
    {
        private readonly ICustomImportProxy _customProxy;
        private readonly ISearchForNewAlbum _albumSearchService;
        public override string Name => "Custom List";

        public override ImportListType ListType => ImportListType.Advanced;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(6);

        public CustomImport(ICustomImportProxy customProxy,
                            ISearchForNewAlbum albumSearchService,
                            IImportListStatusService importListStatusService,
                            IConfigService configService,
                            IParsingService parsingService,
                            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _customProxy = customProxy;
            _albumSearchService = albumSearchService;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var artists = new List<ImportListItemInfo>();

            try
            {
                var remoteSeries = _customProxy.GetArtists(Settings);

                foreach (var item in remoteSeries)
                {
                    var importItem = new ImportListItemInfo();

                    try
                    {
                        var albumQuery = $"lidarr:{item.MusicBrainzId}";
                        var mappedAlbum = _albumSearchService.SearchForNewAlbum(albumQuery, null).FirstOrDefault();

                        if (mappedAlbum != null)
                        {
                            // MusicBrainzId was actually an album ID
                            importItem.AlbumMusicBrainzId = mappedAlbum.ForeignAlbumId;
                            importItem.Album = mappedAlbum.Title;
                            importItem.Artist = mappedAlbum.ArtistMetadata?.Value?.Name;
                            importItem.ArtistMusicBrainzId = mappedAlbum.ArtistMetadata?.Value?.ForeignArtistId;

                            _logger.Debug("Custom List item {0} identified as album: {1} - {2}",
                                item.MusicBrainzId,
                                importItem.Artist,
                                importItem.Album);
                        }
                        else
                        {
                            importItem.ArtistMusicBrainzId = item.MusicBrainzId;

                            _logger.Debug("Custom List item {0} treated as artist ID", item.MusicBrainzId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug(ex, "Failed to search for album with ID {0}, treating as artist", item.MusicBrainzId);

                        importItem.ArtistMusicBrainzId = item.MusicBrainzId;
                    }

                    artists.Add(importItem);
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to fetch data for list {0} ({1})", Definition.Name, Name);

                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(artists);
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            return new { };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(_customProxy.Test(Settings));
        }
    }
}
