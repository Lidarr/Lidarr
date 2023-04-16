using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Lidarr.Api.V1.Profiles.Metadata
{
    public static class MetadataValidation
    {
        public static IRuleBuilderOptions<T, IList<ProfilePrimaryAlbumTypeItemResource>> MustHaveAllowedPrimaryType<T>(this IRuleBuilder<T, IList<ProfilePrimaryAlbumTypeItemResource>> ruleBuilder)
        {
            ruleBuilder.SetValidator(new NotEmptyValidator(null));

            return ruleBuilder.SetValidator(new PrimaryTypeValidator<T>());
        }

        public static IRuleBuilderOptions<T, IList<ProfileSecondaryAlbumTypeItemResource>> MustHaveAllowedSecondaryType<T>(this IRuleBuilder<T, IList<ProfileSecondaryAlbumTypeItemResource>> ruleBuilder)
        {
            ruleBuilder.SetValidator(new NotEmptyValidator(null));

            return ruleBuilder.SetValidator(new SecondaryTypeValidator<T>());
        }

        public static IRuleBuilderOptions<T, IList<ProfileReleaseStatusItemResource>> MustHaveAllowedReleaseStatus<T>(this IRuleBuilder<T, IList<ProfileReleaseStatusItemResource>> ruleBuilder)
        {
            ruleBuilder.SetValidator(new NotEmptyValidator(null));

            return ruleBuilder.SetValidator(new ReleaseStatusValidator<T>());
        }
    }

    public class PrimaryTypeValidator<T> : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Must have at least one allowed primary type";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            return context.PropertyValue is IList<ProfilePrimaryAlbumTypeItemResource> list &&
                   list.Any(c => c.Allowed);
        }
    }

    public class SecondaryTypeValidator<T> : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Must have at least one allowed secondary type";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            return context.PropertyValue is IList<ProfileSecondaryAlbumTypeItemResource> list &&
                   list.Any(c => c.Allowed);
        }
    }

    public class ReleaseStatusValidator<T> : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Must have at least one allowed release status";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            return context.PropertyValue is IList<ProfileReleaseStatusItemResource> list &&
                   list.Any(c => c.Allowed);
        }
    }
}
