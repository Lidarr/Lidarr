using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class AlbumSearchCriteria : SearchCriteriaBase
    {
        public string AlbumTitle { get; set; }
        public List<string> AlbumAliases { get; set; }
        public int AlbumYear { get; set; }
        public string Disambiguation { get; set; }

        public string AlbumQuery => GetQueryTitle(AddDisambiguation(AlbumTitle));

        public List<string> AlbumQueries => OrderQueries(AlbumTitle, AlbumAliases)
            .Select(x => GetQueryTitle(AddDisambiguation(x)))
            .Distinct()
            .ToList();

        private string AddDisambiguation(string term)
        {
            return Disambiguation.IsNullOrWhiteSpace() ? term : $"{term}+{Disambiguation}";
        }

        public override string ToString()
        {
            return $"[{Artist.Name} - {AlbumTitle}{(Disambiguation.IsNullOrWhiteSpace() ? string.Empty : $" ({Disambiguation})")} ({AlbumYear})]";
        }
    }
}
