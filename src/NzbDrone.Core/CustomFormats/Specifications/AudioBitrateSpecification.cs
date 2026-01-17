using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.CustomFormats
{
    public class AudioBitrateSpecificationValidator : AbstractValidator<AudioBitrateSpecification>
    {
        public AudioBitrateSpecificationValidator()
        {
            RuleFor(c => c.Min).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Max).GreaterThanOrEqualTo(c => c.Min);
        }
    }

    public class AudioBitrateSpecification : CustomFormatSpecificationBase
    {
        private static readonly AudioBitrateSpecificationValidator Validator = new ();

        public override int Order => 11;
        public override string ImplementationName => "Audio Bitrate";

        [FieldDefinition(1, Label = "Minimum Bitrate", HelpText = "Minimum bitrate in kbps (e.g., 320 for MP3)", Type = FieldType.Number)]
        public int Min { get; set; }

        [FieldDefinition(2, Label = "Maximum Bitrate", HelpText = "Maximum bitrate in kbps", Type = FieldType.Number)]
        public int Max { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            if (input.MediaInfo == null)
            {
                return false;
            }

            var bitrate = input.MediaInfo.AudioBitrate;
            return bitrate >= Min && bitrate <= Max;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
