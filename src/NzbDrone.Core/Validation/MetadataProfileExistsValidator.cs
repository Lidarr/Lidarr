using FluentValidation.Validators;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.Validation
{
    public class MetadataProfileExistsValidator : PropertyValidator
    {
        private readonly IMetadataProfileService _metadataProfileService;

        public MetadataProfileExistsValidator(IMetadataProfileService metadataProfileService)
        {
            _metadataProfileService = metadataProfileService;
        }

        protected override string GetDefaultMessageTemplate() => "Metadata profile does not exist";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context?.PropertyValue == null || (int)context.PropertyValue == 0)
            {
                return true;
            }

            return _metadataProfileService.Exists((int)context.PropertyValue);
        }
    }
}
