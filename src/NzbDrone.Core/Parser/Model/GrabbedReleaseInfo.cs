using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.History;

namespace NzbDrone.Core.Parser.Model
{
    public class GrabbedReleaseInfo
    {
        public string Title { get; set; }
        public string Indexer { get; set; }
        public long Size { get; set; }

        public List<int> AlbumsIds { get; set; }

        public GrabbedReleaseInfo(List<EntityHistory> grabbedHistories)
        {
            var grabbedHistory = grabbedHistories.MaxBy(h => h.Date);
            var albumsIds = grabbedHistories.Select(h => h.AlbumId).Distinct().ToList();

            grabbedHistory.Data.TryGetValue("indexer", out var indexer);
            grabbedHistory.Data.TryGetValue("size", out var sizeString);
            long.TryParse(sizeString, out var size);

            Title = grabbedHistory.SourceTitle;
            Indexer = indexer;
            Size = size;
            AlbumsIds = albumsIds;
        }
    }
}
