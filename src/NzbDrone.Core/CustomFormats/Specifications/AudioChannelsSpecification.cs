using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.CustomFormats
{
    public class AudioChannelsSpecificationValidator : AbstractValidator<AudioChannelsSpecification>
    {
        public AudioChannelsSpecificationValidator()
        {
            RuleFor(c => c.Min).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Max).GreaterThanOrEqualTo(c => c.Min);
        }
    }

    public class AudioChannelsSpecification : CustomFormatSpecificationBase
    {
        private static readonly AudioChannelsSpecificationValidator Validator = new ();

        public override int Order => 13;
        public override string ImplementationName => "Audio Channels";

        [FieldDefinition(1, Label = "Minimum Channels", HelpText = "Minimum number of audio channels (e.g., 2 for stereo, 6 for 5.1)", Type = FieldType.Number)]
        public int Min { get; set; }

        [FieldDefinition(2, Label = "Maximum Channels", HelpText = "Maximum number of audio channels", Type = FieldType.Number)]
        public int Max { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            if (input.MediaInfo == null)
            {
                return false;
            }

            var channels = input.MediaInfo.AudioChannels;
            return channels >= Min && channels <= Max;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
