using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class AlbumSearchCriteria : SearchCriteriaBase
    {
        public string AlbumTitle { get; set; }
        public int AlbumYear { get; set; }
        public string Disambiguation { get; set; }

        private string _albumQuery;
        public string AlbumQuery
        {
            get => _albumQuery ?? $"{AlbumTitle}{(Disambiguation.IsNullOrWhiteSpace() ? string.Empty : $"+{Disambiguation}")}";
            set => _albumQuery = value;
        }

        private string _cleanAlbumQuery;
        public string CleanAlbumQuery
        {
            get => _cleanAlbumQuery ?? (AlbumQuery != null ? GetQueryTitle(AlbumQuery) : null);
            set => _cleanAlbumQuery = value;
        }

        public override string ToString()
        {
            return $"[{Artist.Name} - {AlbumTitle}{(Disambiguation.IsNullOrWhiteSpace() ? string.Empty : $" ({Disambiguation})")} ({AlbumYear})]";
        }
    }
}
