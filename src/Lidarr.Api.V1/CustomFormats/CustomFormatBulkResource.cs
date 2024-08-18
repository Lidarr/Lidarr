using System.Collections.Generic;

namespace Lidarr.Api.V1.CustomFormats
{
    public class CustomFormatBulkResource
    {
        public HashSet<int> Ids { get; set; } = new ();
        public bool? IncludeCustomFormatWhenRenaming { get; set; }
    }
}
