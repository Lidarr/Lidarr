using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Lidarr
{
    public class LidarrImport : ImportListBase<LidarrSettings>
    {
        private readonly ILidarrV1Proxy _lidarrV1Proxy;
        public override string Name => "Lidarr";

        public override ImportListType ListType => ImportListType.Program;

        public LidarrImport(ILidarrV1Proxy lidarrV1Proxy,
                            IImportListStatusService importListStatusService,
                            IConfigService configService,
                            IParsingService parsingService,
                            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _lidarrV1Proxy = lidarrV1Proxy;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var artistsAndAlbums = new List<ImportListItemInfo>();

            try
            {
                var remoteAlbums = _lidarrV1Proxy.GetAlbums(Settings);
                var remoteArtists = _lidarrV1Proxy.GetArtists(Settings);

                var artistDict = remoteArtists.ToDictionary(x => x.Id);

                foreach (var remoteAlbum in remoteAlbums)
                {
                    var remoteArtist = remoteArtists[remoteAlbum.ArtistId];
                    if ((!Settings.ProfileIds.Any() || Settings.ProfileIds.Contains(remoteArtist.QualityProfileId)) &&
                        (!Settings.TagIds.Any() || Settings.TagIds.Any(x => remoteArtist.Tags.Any(y => y == x))) &&
                         remoteAlbum.Monitored && remoteArtist.Monitored)
                    {
                        artistsAndAlbums.Add(new ImportListItemInfo
                        {
                            ArtistMusicBrainzId = remoteArtist.ForeignArtistId,
                            Artist = remoteArtist.ArtistName,
                            AlbumMusicBrainzId = remoteAlbum.ForeignAlbumId,
                            Album = remoteAlbum.Title
                        });
                    }
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(artistsAndAlbums);
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            // Return early if there is not an API key
            if (Settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new
                {
                    devices = new List<object>()
                };
            }

            Settings.Validate().Filter("ApiKey").ThrowOnError();

            if (action == "getProfiles")
            {
                var devices = _lidarrV1Proxy.GetProfiles(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Name, StringComparer.InvariantCultureIgnoreCase)
                                            .Select(d => new
                                            {
                                                Value = d.Id,
                                                Name = d.Name
                                            })
                };
            }

            if (action == "getTags")
            {
                var devices = _lidarrV1Proxy.GetTags(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Label, StringComparer.InvariantCultureIgnoreCase)
                                            .Select(d => new
                                            {
                                                Value = d.Id,
                                                Name = d.Label
                                            })
                };
            }

            return new { };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(_lidarrV1Proxy.Test(Settings));
        }
    }
}
