using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Aggregation.Aggregators
{
    // public class AggregateReleaseInfo : IAggregateLocalEpisode
    // {
    //     private readonly IHistoryService _historyService;
    //
    //     public AggregateReleaseInfo(IHistoryService historyService)
    //     {
    //         _historyService = historyService;
    //     }
    //
    //     public LocalTrack Aggregate(LocalTrack localTrack, DownloadClientItem downloadClientItem)
    //     {
    //         if (downloadClientItem == null)
    //         {
    //             return localTrack;
    //         }
    //
    //         var grabbedHistories = _historyService.FindByDownloadId(downloadClientItem.DownloadId)
    //             .Where(h => h.EventType == TrackHistoryEventType.Grabbed)
    //             .ToList();
    //
    //         if (grabbedHistories.Empty())
    //         {
    //             return localTrack;
    //         }
    //
    //         localTrack.Release = new GrabbedReleaseInfo(grabbedHistories);
    //
    //         return localTrack;
    //     }
    // }
}
