using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Xbmc.Model
{
    public class SourceResult
    {
        public Dictionary<string, int> Limits { get; set; }
        public List<KodiSource> Sources;

        public SourceResult()
        {
            Sources = new List<KodiSource>();
        }
    }
}
