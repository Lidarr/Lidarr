using System;

namespace NzbDrone.Common
{
    /// <summary>
    /// Limits parallel disk work during library scan/import (tag reads, folder scans, candidate scoring).
    /// Unrelated to download bandwidth limits in Lidarr settings.
    /// </summary>
    public static class MediaImportParallelism
    {
        public const string EnvironmentVariableName = "LIDARR_MEDIA_IO_PARALLELISM";

        private const int DefaultMaxDegree = 2;
        private const int MinDegree = 1;
        private const int MaxDegreeCap = 64;

        private static readonly Lazy<int> MaxDegreeLazy = new Lazy<int>(ReadMaxDegree);

        /// <summary>
        /// Maximum concurrent workers for scan/import parallelism. Default 2; override with LIDARR_MEDIA_IO_PARALLELISM (1–64).
        /// </summary>
        public static int MaxDegreeOfParallelism => MaxDegreeLazy.Value;

        private static int ReadMaxDegree()
        {
            var raw = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw.Trim(), out var parsed))
            {
                return DefaultMaxDegree;
            }

            if (parsed < MinDegree)
            {
                return DefaultMaxDegree;
            }

            return Math.Min(parsed, MaxDegreeCap);
        }
    }
}
