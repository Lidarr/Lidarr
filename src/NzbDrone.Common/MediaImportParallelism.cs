using System;
using System.Threading.Tasks;

namespace NzbDrone.Common
{
    /// <summary>
    /// Optional cap on parallel scan/import work via <c>LIDARR_MEDIA_IO_PARALLELISM</c>.
    /// Unrelated to download bandwidth limits in Lidarr settings.
    /// </summary>
    public static class MediaImportParallelism
    {
        public const string EnvironmentVariableName = "LIDARR_MEDIA_IO_PARALLELISM";

        private const int MaxDegreeCap = 64;

        /// <summary>
        /// <b>Unset / empty / invalid / ≤0:</b> Original fork behavior — no explicit cap on
        /// <see cref="ParallelOptions.MaxDegreeOfParallelism"/> (TPL default <c>-1</c>, scheduler chooses concurrency; often higher than core count for I/O).
        /// <b>1–64:</b> Cap <see cref="Parallel.ForEach"/> loops to that many concurrent workers (use on NFS / slow storage).
        /// </summary>
        public static ParallelOptions GetParallelForEachOptions()
        {
            if (!TryParseUserCap(out var cap))
            {
                return new ParallelOptions();
            }

            return new ParallelOptions { MaxDegreeOfParallelism = cap };
        }

        /// <summary>
        /// PLINQ <c>WithDegreeOfParallelism</c> must be ≥ 1.
        /// <b>Uncapped:</b> <see cref="Environment.ProcessorCount"/> (same as pre-env ImportDecisionMaker / IdentificationService).
        /// <b>Capped:</b> user value (1–64).
        /// </summary>
        public static int PlinqMaxDegreeOfParallelism
        {
            get
            {
                if (!TryParseUserCap(out var cap))
                {
                    return Math.Max(1, Environment.ProcessorCount);
                }

                return cap;
            }
        }

        /// <summary>
        /// For logging: -1 means TPL default (uncapped loops); otherwise the explicit cap.
        /// </summary>
        public static int EffectiveParallelForEachDegreeForLog =>
            TryParseUserCap(out var cap) ? cap : -1;

        private static bool TryParseUserCap(out int cap)
        {
            cap = 0;
            var raw = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw.Trim(), out var parsed))
            {
                return false;
            }

            if (parsed <= 0)
            {
                return false;
            }

            cap = Math.Min(parsed, MaxDegreeCap);
            return true;
        }
    }
}
