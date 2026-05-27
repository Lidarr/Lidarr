using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.MediaFiles
{
    public class TrackFileFilter : ITrackFileFilter
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public TrackFileFilter(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public bool IsExcluded(string basePath, string fullPath)
        {
            var excludedFolders = GetExcludedFolders();

            if (excludedFolders.Count == 0)
            {
                return false;
            }

            var relativePath = basePath.GetRelativePath(fullPath);

            var segments = relativePath.Split(
                new[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.RemoveEmptyEntries);

            return segments.Any(segment =>
                excludedFolders.Contains(segment.Trim()));
        }

        private HashSet<string> GetExcludedFolders()
        {
            var value = _configService.ExcludedScanFolders;

            _logger.Debug("ExcludedScanFolders raw config value: '{0}'", value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var result = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _logger.Debug("ExcludedScanFolders parsed list: [{0}]", string.Join(", ", result));

            return result;
        }
    }
}
