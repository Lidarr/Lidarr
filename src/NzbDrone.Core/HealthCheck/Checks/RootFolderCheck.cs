using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ArtistsDeletedEvent))]
    [CheckOn(typeof(ArtistMovedEvent))]
    [CheckOn(typeof(TrackImportedEvent), CheckOnCondition.FailedOnly)]
    [CheckOn(typeof(TrackImportFailedEvent), CheckOnCondition.SuccessfulOnly)]
    public class RootFolderCheck : HealthCheckBase
    {
        private readonly IArtistService _artistService;
        private readonly IImportListFactory _importListFactory;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderService _rootFolderService;

        public RootFolderCheck(IArtistService artistService, IImportListFactory importListFactory, IDiskProvider diskProvider, IRootFolderService rootFolderService)
        {
            _artistService = artistService;
            _importListFactory = importListFactory;
            _diskProvider = diskProvider;
            _rootFolderService = rootFolderService;
        }

        public override HealthCheck Check()
        {
            var rootFolders = _artistService.AllArtistPaths()
                                                           .Select(s => _rootFolderService.GetBestRootFolderPath(s.Value))
                                                           .Distinct();

            var missingRootFolders = rootFolders.Where(s => !_diskProvider.FolderExists(s))
                                                          .ToList();

            missingRootFolders.AddRange(_importListFactory.All()
                .Select(s => s.RootFolderPath)
                .Distinct()
                .Where(s => !_diskProvider.FolderExists(s))
                .ToList());

            missingRootFolders = missingRootFolders.Distinct().ToList();

            if (missingRootFolders.Any())
            {
                if (missingRootFolders.Count == 1)
                {
                    return new HealthCheck(GetType(), HealthCheckResult.Error, "Missing root folder: " + missingRootFolders.First(), "#missing_root_folder");
                }

                var message = string.Format("Multiple root folders are missing: {0}", string.Join(" | ", missingRootFolders));
                return new HealthCheck(GetType(), HealthCheckResult.Error, message, "#missing_root_folder");
            }

            return new HealthCheck(GetType());
        }
    }
}
