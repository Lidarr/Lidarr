using System;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public class DownloadClientDefinition : ProviderDefinition
    {
        public string Protocol { get; set; }
        public int Priority { get; set; } = 1;
    }
}
