using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Music;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.AutoTagging.Specifications
{
    public class MetadataProfileSpecificationValidator : AbstractValidator<MetadataProfileSpecification>
    {
        public MetadataProfileSpecificationValidator()
        {
            RuleFor(c => c.Value).GreaterThan(0);
        }
    }

    public class MetadataProfileSpecification : AutoTaggingSpecificationBase
    {
        private static readonly MetadataProfileSpecificationValidator Validator = new ();

        public override int Order => 1;
        public override string ImplementationName => "Metadata Profile";

        [FieldDefinition(1, Label = "Metadata Profile", Type = FieldType.MetadataProfile)]
        public int Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Artist artist)
        {
            return Value == artist.MetadataProfileId;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
