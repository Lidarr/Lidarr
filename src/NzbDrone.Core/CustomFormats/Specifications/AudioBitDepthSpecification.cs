using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.CustomFormats
{
    public class AudioBitDepthSpecificationValidator : AbstractValidator<AudioBitDepthSpecification>
    {
        public AudioBitDepthSpecificationValidator()
        {
            RuleFor(c => c.Min).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Max).GreaterThanOrEqualTo(c => c.Min);
        }
    }

    public class AudioBitDepthSpecification : CustomFormatSpecificationBase
    {
        private static readonly AudioBitDepthSpecificationValidator Validator = new ();

        public override int Order => 10;
        public override string ImplementationName => "Audio Bit Depth";

        [FieldDefinition(1, Label = "Minimum Bit Depth", HelpText = "Minimum bit depth (e.g., 16 for CD quality, 24 for Hi-Res)", Type = FieldType.Number)]
        public int Min { get; set; }

        [FieldDefinition(2, Label = "Maximum Bit Depth", HelpText = "Maximum bit depth", Type = FieldType.Number)]
        public int Max { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            if (input.MediaInfo == null)
            {
                return false;
            }

            var bitDepth = input.MediaInfo.AudioBits;
            return bitDepth >= Min && bitDepth <= Max;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
