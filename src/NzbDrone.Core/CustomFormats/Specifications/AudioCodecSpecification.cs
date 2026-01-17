using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.CustomFormats
{
    public class AudioCodecSpecificationValidator : AbstractValidator<AudioCodecSpecification>
    {
        public AudioCodecSpecificationValidator()
        {
            RuleFor(c => c.Value).NotEmpty().WithMessage("Audio Codec must not be empty");
        }
    }

    public class AudioCodecSpecification : CustomFormatSpecificationBase
    {
        private static readonly AudioCodecSpecificationValidator Validator = new ();

        protected Regex _regex;
        protected string _raw;

        public override int Order => 12;
        public override string ImplementationName => "Audio Codec";

        [FieldDefinition(1, Label = "Audio Codec", HelpText = "Codec name or regex pattern (e.g., FLAC, MP3, AAC, ALAC, OPUS, WavPack, APE)", Type = FieldType.Textbox)]
        public string Value
        {
            get => _raw;
            set
            {
                _raw = value;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    _regex = new Regex(value, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
            }
        }

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            if (input.MediaInfo == null || string.IsNullOrWhiteSpace(input.MediaInfo.AudioFormat))
            {
                return false;
            }

            if (_regex == null)
            {
                return false;
            }

            return _regex.IsMatch(input.MediaInfo.AudioFormat);
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
