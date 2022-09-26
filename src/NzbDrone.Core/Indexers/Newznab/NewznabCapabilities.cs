﻿using System.Collections.Generic;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class NewznabCapabilities
    {
        public int DefaultPageSize { get; set; }
        public int MaxPageSize { get; set; }
        public string[] SupportedSearchParameters { get; set; }
        public string[] SupportedTvSearchParameters { get; set; }
        public string[] SupportedAudioSearchParameters { get; set; }
        public bool SupportsAggregateIdSearch { get; set; }
        public string TextSearchEngine { get; set; }
        public string AudioTextSearchEngine { get; set; }
        public List<NewznabCategory> Categories { get; set; }

        public NewznabCapabilities()
        {
            DefaultPageSize = 100;
            MaxPageSize = 100;
            SupportedSearchParameters = new[] { "q" };
            SupportedTvSearchParameters = new[] { "q", "rid", "season", "ep" }; // This should remain 'rid' for older newznab installs.
            SupportedAudioSearchParameters = new[] { "q", "artist", "album" };
            SupportsAggregateIdSearch = false;
            TextSearchEngine = "sphinx"; // This should remain 'sphinx' for older newznab installs
            AudioTextSearchEngine = "sphinx"; // This should remain 'sphinx' for older newznab installs
            Categories = new List<NewznabCategory>();
        }
    }

    public class NewznabCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<NewznabCategory> Subcategories { get; set; }
    }
}
