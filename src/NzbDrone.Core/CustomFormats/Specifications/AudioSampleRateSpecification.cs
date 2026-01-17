using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.CustomFormats
{
    public class AudioSampleRateSpecificationValidator : AbstractValidator<AudioSampleRateSpecification>
    {
        public AudioSampleRateSpecificationValidator()
        {
            RuleFor(c => c.Min).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Max).GreaterThanOrEqualTo(c => c.Min);
        }
    }

    public class AudioSampleRateSpecification : CustomFormatSpecificationBase
    {
        private static readonly AudioSampleRateSpecificationValidator Validator = new ();

        public override int Order => 9;
        public override string ImplementationName => "Audio Sample Rate";

        [FieldDefinition(1, Label = "Minimum Sample Rate", HelpText = "Minimum sample rate in Hz (e.g., 44100 for CD quality, 192000 for Hi-Res)", Type = FieldType.Number)]
        public int Min { get; set; }

        [FieldDefinition(2, Label = "Maximum Sample Rate", HelpText = "Maximum sample rate in Hz", Type = FieldType.Number)]
        public int Max { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            if (input.MediaInfo == null)
            {
                return false;
            }

            var sampleRate = input.MediaInfo.AudioSampleRate;
            return sampleRate >= Min && sampleRate <= Max;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
