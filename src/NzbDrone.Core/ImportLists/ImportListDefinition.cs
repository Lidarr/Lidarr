using System;
using NzbDrone.Core.Music;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListDefinition : ProviderDefinition
    {
        public bool EnableAutomaticAdd { get; set; }
        public ImportListMonitorType ShouldMonitor { get; set; }
        public bool ShouldMonitorExisting { get; set; }
        public NewItemMonitorTypes MonitorNewItems { get; set; }
        public bool ShouldSearch { get; set; }
        public int ProfileId { get; set; }
        public int MetadataProfileId { get; set; }
        public string RootFolderPath { get; set; }

        public override bool Enable => EnableAutomaticAdd;

        public ImportListStatus Status { get; set; }
        public ImportListType ListType { get; set; }
        public TimeSpan MinRefreshInterval { get; set; }
    }

    public enum ImportListMonitorType
    {
        None,
        SpecificAlbum,
        EntireArtist
    }
}
