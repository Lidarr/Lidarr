using Lidarr.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace Lidarr.Api.V1.ImportLists
{
    [V1ApiController]
    public class ImportListController : ProviderControllerBase<ImportListResource, ImportListBulkResource, IImportList, ImportListDefinition>
    {
        public static readonly ImportListResourceMapper ResourceMapper = new ();
        public static readonly ImportListBulkResourceMapper BulkResourceMapper = new ();

        public ImportListController(IImportListFactory importListFactory,
                                    QualityProfileExistsValidator qualityProfileExistsValidator,
                                    MetadataProfileExistsValidator metadataProfileExistsValidator)
            : base(importListFactory, "importlist", ResourceMapper, BulkResourceMapper)
        {
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.QualityProfileId));
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.MetadataProfileId));

            SharedValidator.RuleFor(c => c.RootFolderPath).IsValidPath();
            SharedValidator.RuleFor(c => c.QualityProfileId).SetValidator(qualityProfileExistsValidator);
            SharedValidator.RuleFor(c => c.MetadataProfileId).SetValidator(metadataProfileExistsValidator);
        }
    }
}
