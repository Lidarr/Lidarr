using System;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Music;

namespace Lidarr.Api.V1.ImportLists
{
    public class ImportListResource : ProviderResource<ImportListResource>
    {
        public bool EnableAutomaticAdd { get; set; }
        public ImportListMonitorType ShouldMonitor { get; set; }
        public bool ShouldMonitorExisting { get; set; }
        public bool ShouldSearch { get; set; }
        public string RootFolderPath { get; set; }
        public NewItemMonitorTypes MonitorNewItems { get; set; }
        public int QualityProfileId { get; set; }
        public int MetadataProfileId { get; set; }
        public ImportListType ListType { get; set; }
        public int ListOrder { get; set; }
        public TimeSpan MinRefreshInterval { get; set; }
    }

    public class ImportListResourceMapper : ProviderResourceMapper<ImportListResource, ImportListDefinition>
    {
        public override ImportListResource ToResource(ImportListDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var resource = base.ToResource(definition);

            resource.EnableAutomaticAdd = definition.EnableAutomaticAdd;
            resource.ShouldMonitor = definition.ShouldMonitor;
            resource.ShouldMonitorExisting = definition.ShouldMonitorExisting;
            resource.ShouldSearch = definition.ShouldSearch;
            resource.RootFolderPath = definition.RootFolderPath;
            resource.MonitorNewItems = definition.MonitorNewItems;
            resource.QualityProfileId = definition.ProfileId;
            resource.MetadataProfileId = definition.MetadataProfileId;
            resource.ListType = definition.ListType;
            resource.ListOrder = (int)definition.ListType;
            resource.MinRefreshInterval = definition.MinRefreshInterval;

            return resource;
        }

        public override ImportListDefinition ToModel(ImportListResource resource, ImportListDefinition existingDefinition)
        {
            if (resource == null)
            {
                return null;
            }

            var definition = base.ToModel(resource, existingDefinition);

            definition.EnableAutomaticAdd = resource.EnableAutomaticAdd;
            definition.ShouldMonitor = resource.ShouldMonitor;
            definition.ShouldMonitorExisting = resource.ShouldMonitorExisting;
            definition.ShouldSearch = resource.ShouldSearch;
            definition.RootFolderPath = resource.RootFolderPath;
            definition.MonitorNewItems = resource.MonitorNewItems;
            definition.ProfileId = resource.QualityProfileId;
            definition.MetadataProfileId = resource.MetadataProfileId;
            definition.ListType = resource.ListType;
            definition.MinRefreshInterval = resource.MinRefreshInterval;

            return definition;
        }
    }
}
