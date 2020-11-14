using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Indexers.Newznab
{
    public static class NewznabCategoryFieldOptionsConverter
    {
        public static List<FieldSelectOption> GetFieldSelectOptions(List<NewznabCategory> categories)
        {
            // Ignore categories not relevant for Lidarr
            var ignoreCategories = new[] { 1000, 2000, 4000, 5000, 6000, 7000 };

            // And maybe relevant for specific users
            var unimportantCategories = new[] { 0 };

            var result = new List<FieldSelectOption>();

            if (categories == null)
            {
                // Fetching categories failed, use default Newznab categories
                categories = new List<NewznabCategory>();
                categories.Add(new NewznabCategory
                {
                    Id = 3000,
                    Name = "Music",
                    Subcategories = new List<NewznabCategory>
                    {
                        new NewznabCategory { Id = 3040, Name = "Loseless" },
                        new NewznabCategory { Id = 3010, Name = "MP3" },
                        new NewznabCategory { Id = 3050, Name = "Other" },
                        new NewznabCategory { Id = 3030, Name = "Audiobook" }
                    }
                });
            }

            foreach (var category in categories.Where(cat => !ignoreCategories.Contains(cat.Id)).OrderBy(cat => unimportantCategories.Contains(cat.Id)).ThenBy(cat => cat.Id))
            {
                result.Add(new FieldSelectOption
                {
                    Value = category.Id,
                    Name = category.Name,
                    Hint = $"({category.Id})"
                });

                if (category.Subcategories != null)
                {
                    foreach (var subcat in category.Subcategories.OrderBy(cat => cat.Id))
                    {
                        result.Add(new FieldSelectOption
                        {
                            Value = subcat.Id,
                            Name = subcat.Name,
                            Hint = $"({subcat.Id})",
                            ParentValue = category.Id
                        });
                    }
                }
            }

            return result;
        }
    }
}
