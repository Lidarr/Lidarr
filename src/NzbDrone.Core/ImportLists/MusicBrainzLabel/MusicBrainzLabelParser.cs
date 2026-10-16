using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.MusicBrainzLabel
{
    public class MusicBrainzLabelParser : IParseImportListResponse
    {
        // MusicBrainz' single canonical "Various Artists" artist.
        private const string VariousArtistsId = "89ad4ac3-39f7-470e-963a-56509c546377";

        private readonly MusicBrainzLabelSettings _settings;
        private readonly Logger _logger;

        private readonly HashSet<string> _primaryTypes;
        private readonly HashSet<string> _excludedSecondaryTypes;

        // A label puts out many releases of the same album — pressings, reissues,
        // territory variants. Lidarr wants one item per release group, and this
        // parser instance lives for the whole fetch, so it dedupes across pages.
        private readonly HashSet<string> _seenReleaseGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MusicBrainzLabelParser(MusicBrainzLabelSettings settings, Logger logger)
        {
            _settings = settings;
            _logger = logger;
            _primaryTypes = settings.GetPrimaryTypeNames();
            _excludedSecondaryTypes = settings.GetExcludedSecondaryTypeNames();
        }

        public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
        {
            var items = new List<ImportListItemInfo>();

            if (!PreProcess(importListResponse))
            {
                return items;
            }

            var jsonResponse = JsonConvert.DeserializeObject<MusicBrainzLabelBrowseResponse>(importListResponse.Content);

            if (jsonResponse?.Releases == null)
            {
                return items;
            }

            foreach (var release in jsonResponse.Releases)
            {
                var item = MapRelease(release);

                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private ImportListItemInfo MapRelease(MusicBrainzLabelRelease release)
        {
            var releaseGroup = release?.ReleaseGroup;

            if (releaseGroup?.Id == null || releaseGroup.Title.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (_seenReleaseGroups.Contains(releaseGroup.Id))
            {
                return null;
            }

            // Status lives on the release, not the group, so a group first seen as
            // a promo can still qualify via a later official pressing. That's why
            // nothing is marked as seen until it's actually accepted.
            if (_settings.OfficialReleasesOnly && !"Official".Equals(release.Status, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!_primaryTypes.Contains(releaseGroup.PrimaryType ?? string.Empty))
            {
                return null;
            }

            if (releaseGroup.SecondaryTypes?.Any(t => _excludedSecondaryTypes.Contains(t)) == true)
            {
                return null;
            }

            var artist = (releaseGroup.ArtistCredit ?? release.ArtistCredit)?.FirstOrDefault()?.Artist;

            if (artist?.Name.IsNullOrWhiteSpace() != false)
            {
                return null;
            }

            if (_settings.ExcludeVariousArtists &&
                (VariousArtistsId.Equals(artist.Id, StringComparison.OrdinalIgnoreCase) ||
                 "Various Artists".Equals(artist.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var releaseDate = ParseDate(releaseGroup.FirstReleaseDate);

            // Undated groups are usually forthcoming rather than ancient, so a
            // minimum-year filter lets them through rather than hiding new signings.
            if (_settings.MinimumYear.HasValue &&
                releaseDate.HasValue &&
                releaseDate.Value.Year < _settings.MinimumYear.Value)
            {
                return null;
            }

            _seenReleaseGroups.Add(releaseGroup.Id);

            return new ImportListItemInfo
            {
                Artist = artist.Name,
                ArtistMusicBrainzId = artist.Id,
                Album = releaseGroup.Title,
                AlbumMusicBrainzId = releaseGroup.Id,
                ReleaseDate = releaseDate.GetValueOrDefault()
            };
        }

        // MusicBrainz dates degrade gracefully: "1982", "1982-10" or "1982-10-01".
        private static DateTime? ParseDate(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return null;
            }

            var formats = new[] { "yyyy-MM-dd", "yyyy-MM", "yyyy" };

            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private bool PreProcess(ImportListResponse importListResponse)
        {
            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse,
                    "MusicBrainz API call resulted in an unexpected StatusCode [{0}]",
                    importListResponse.HttpResponse.StatusCode);
            }

            if (importListResponse.HttpResponse.Headers.ContentType != null &&
                importListResponse.HttpResponse.Headers.ContentType.Contains("text/html"))
            {
                throw new ImportListException(importListResponse,
                    "MusicBrainz responded with HTML content. The server is likely rate limiting or unavailable.");
            }

            return true;
        }
    }
}
