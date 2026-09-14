using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class AlbumSearchCriteria : SearchCriteriaBase
    {
        public string AlbumTitle { get; set; }
        public int AlbumYear { get; set; }
        public string Disambiguation { get; set; }

        public string AlbumQuery => $"{AlbumTitle}{(Disambiguation.IsNullOrWhiteSpace() ? string.Empty : $"+{Disambiguation}")}";
        public string CleanAlbumQuery => GetQueryTitle(AlbumQuery);

        public override string ToString()
        {
            return $"[{Artist.SearchName} - {AlbumTitle}{(Disambiguation.IsNullOrWhiteSpace() ? string.Empty : $" ({Disambiguation})")} ({AlbumYear})]";
        }
    }
}
