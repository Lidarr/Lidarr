using System;

namespace NzbDrone.Common
{
    /// <summary>
    /// Caps parallel disk work during library scan/import (tag reads, folder scans, candidate scoring), optional via env.
    /// Unrelated to download bandwidth limits in Lidarr settings.
    /// </summary>
    public static class MediaImportParallelism
    {
        public const string EnvironmentVariableName = "LIDARR_MEDIA_IO_PARALLELISM";

        private const int MaxDegreeCap = 64;

        /// <summary>
        /// Maximum concurrent workers for scan/import parallelism.
        /// If <c>LIDARR_MEDIA_IO_PARALLELISM</c> is unset, empty, invalid, 0, or negative: uses <see cref="Environment.ProcessorCount"/> (matches pre-cap fork behavior).
        /// Otherwise uses the set value clamped to 1–64.
        /// Re-reads the environment each call so container/env changes are visible without restart (same process still needs a new read on next scan).
        /// </summary>
        public static int MaxDegreeOfParallelism => ReadMaxDegree();

        private static int ReadMaxDegree()
        {
            var raw = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw.Trim(), out var parsed))
            {
                return Math.Max(1, Environment.ProcessorCount);
            }

            if (parsed <= 0)
            {
                return Math.Max(1, Environment.ProcessorCount);
            }

            return Math.Min(parsed, MaxDegreeCap);
        }
    }
}
