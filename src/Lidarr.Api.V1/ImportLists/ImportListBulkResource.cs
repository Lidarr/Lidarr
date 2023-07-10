using System.Collections.Generic;
using NzbDrone.Core.ImportLists;

namespace Lidarr.Api.V1.ImportLists
{
    public class ImportListBulkResource : ProviderBulkResource<ImportListBulkResource>
    {
        public bool? EnableAutomaticAdd { get; set; }
        public string RootFolderPath { get; set; }
        public int? QualityProfileId { get; set; }
    }

    public class ImportListBulkResourceMapper : ProviderBulkResourceMapper<ImportListBulkResource, ImportListDefinition>
    {
        public override List<ImportListDefinition> UpdateModel(ImportListBulkResource resource, List<ImportListDefinition> existingDefinitions)
        {
            if (resource == null)
            {
                return new List<ImportListDefinition>();
            }

            existingDefinitions.ForEach(existing =>
            {
                existing.EnableAutomaticAdd = resource.EnableAutomaticAdd ?? existing.EnableAutomaticAdd;
                existing.RootFolderPath = resource.RootFolderPath ?? existing.RootFolderPath;
                existing.ProfileId = resource.QualityProfileId ?? existing.ProfileId;
            });

            return existingDefinitions;
        }
    }
}
