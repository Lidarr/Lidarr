using FluentValidation.Validators;
using NzbDrone.Core.ImportLists.Exclusions;

namespace Lidarr.Api.V1.ImportLists
{
    public class ImportListExclusionExistsValidator : PropertyValidator
    {
        private readonly IImportListExclusionService _importListExclusionService;

        public ImportListExclusionExistsValidator(IImportListExclusionService importListExclusionService)
        {
            _importListExclusionService = importListExclusionService;
        }

        protected override string GetDefaultMessageTemplate() => "This exclusion has already been added.";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            if (context.InstanceToValidate is not ImportListExclusionResource listExclusionResource)
            {
                return true;
            }

            return !_importListExclusionService.All().Exists(v => v.ForeignId == context.PropertyValue.ToString() && v.Id != listExclusionResource.Id);
        }
    }
}
