using System;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Music
{
    /// <summary>
    /// Helpers for the per-album release filter (preferred track counts / medium counts).
    /// A release is "allowed" when it satisfies the filter, and "wanted" when it is either the
    /// monitored release or (with AnyReleaseOk) any allowed release.
    /// </summary>
    public static class AlbumReleaseFilter
    {
        public static bool HasReleaseFilter(this Album album)
        {
            return album != null &&
                   ((album.PreferredTrackCounts?.Any() ?? false) ||
                    (album.PreferredMediumCounts?.Any() ?? false));
        }

        public static int AudioMediumCount(this AlbumRelease release)
        {
            return release?.Media?.Count(m => m.Number > 0) ?? 0;
        }

        public static bool IsAllowedRelease(this Album album, AlbumRelease release)
        {
            if (release == null)
            {
                return false;
            }

            if (album == null)
            {
                return true;
            }

            var trackCounts = album.PreferredTrackCounts;
            if (trackCounts != null && trackCounts.Any() && !trackCounts.Contains(release.TrackCount))
            {
                return false;
            }

            var mediumCounts = album.PreferredMediumCounts;
            if (mediumCounts != null && mediumCounts.Any() && !mediumCounts.Contains(release.AudioMediumCount()))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Releases matching the filter. Falls back to all releases when the filter matches nothing,
        /// so that a stale filter can never leave an album with no usable release.
        /// </summary>
        public static List<AlbumRelease> GetAllowedReleases(this Album album, IEnumerable<AlbumRelease> releases)
        {
            var all = releases?.ToList() ?? new List<AlbumRelease>();

            if (!album.HasReleaseFilter())
            {
                return all;
            }

            var allowed = all.Where(album.IsAllowedRelease).ToList();

            return allowed.Any() ? allowed : all;
        }

        public static List<AlbumRelease> GetAllowedReleases(this Album album)
        {
            return album.GetAllowedReleases(album?.AlbumReleases?.Value);
        }

        /// <summary>
        /// Whether a release should be accepted for this album: the monitored release always is,
        /// otherwise any allowed release is when automatic release switching is on.
        /// </summary>
        public static bool IsReleaseWanted(this Album album, AlbumRelease release)
        {
            if (release == null)
            {
                return false;
            }

            if (release.Monitored)
            {
                return true;
            }

            return album != null && album.AnyReleaseOk && album.IsAllowedRelease(release);
        }

        public static List<AlbumRelease> GetWantedReleases(this Album album)
        {
            var releases = album?.AlbumReleases?.Value ?? new List<AlbumRelease>();
            return releases.Where(album.IsReleaseWanted).ToList();
        }

        /// <summary>
        /// Picks the single release to monitor from the candidates, respecting the album's filter.
        /// Preference order: already monitored, most files on disk, most tracks.
        /// </summary>
        public static AlbumRelease ChooseMonitoredRelease(this Album album, List<AlbumRelease> releases, Func<AlbumRelease, int> fileCount = null)
        {
            if (releases == null || !releases.Any())
            {
                return null;
            }

            fileCount ??= _ => 0;

            var allowed = album.GetAllowedReleases(releases);

            var monitored = allowed.Where(x => x.Monitored).ToList();
            if (!monitored.Any())
            {
                monitored = allowed;
            }

            return monitored.OrderByDescending(fileCount)
                .ThenByDescending(x => x.TrackCount)
                .First();
        }
    }
}
