using NzbDrone.Common.Extensions;
using System;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class AlbumSearchCriteria : SearchCriteriaBase
    {

        public string AlbumTitle { get; set; }
        public int AlbumYear { get; set; }
        public string Disambiguation { get; set; }

        public string AlbumQuery => GetQueryTitle(AlbumTitle);
        public string DisambiguationQuery => GetQueryTitle(Disambiguation ?? string.Empty);

        public override string ToString()
        {
            return $"[{Artist.Name} - {AlbumTitle} {(Disambiguation.IsNullOrWhiteSpace() ? string.Empty : $"({Disambiguation})")} ({AlbumYear})]";
        }
    }
}
