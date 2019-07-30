using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace Lidarr.Api.V1.ImportLists
{
    public class ImportListModule : ProviderModuleBase<ImportListResource, IImportList, ImportListDefinition>
    {
        public static readonly ImportListResourceMapper ResourceMapper = new ImportListResourceMapper();

        public ImportListModule(ImportListFactory importListFactory,
                                ProfileExistsValidator profileExistsValidator,
                                MetadataProfileExistsValidator metadataProfileExistsValidator
            )
            : base(importListFactory, "importlist", ResourceMapper)
        {
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.QualityProfileId));
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.MetadataProfileId));

            SharedValidator.RuleFor(c => c.RootFolderPath).IsValidPath();
            SharedValidator.RuleFor(c => c.QualityProfileId).SetValidator(profileExistsValidator);
            SharedValidator.RuleFor(c => c.MetadataProfileId).SetValidator(metadataProfileExistsValidator);
        }

        protected override void Validate(ImportListDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable)
            {
                return;
            }
            base.Validate(definition, includeWarnings);
        }
    }
}
