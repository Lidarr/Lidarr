using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public class GazelleSettingsValidator : AbstractValidator<GazelleSettings>
    {
        public GazelleSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class GazelleSettings : IProviderConfig
    {
        private static readonly GazelleSettingsValidator Validator = new GazelleSettingsValidator();

        public GazelleSettings()
        {
            BaseUrl = "https://apollo.rip";
        }

        public string AuthKey;
        public string PassKey;

        [FieldDefinition(0, Label = "URL", Advanced = true, HelpText = "Do not change this unless you know what you're doing. Since your cookie will be sent to that host.")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Username", HelpText = "PTP Username")]
        public string Username { get; set; }

        [FieldDefinition(2, Label = "Password", Type = FieldType.Password, HelpText = "PTP Password")]
        public string Password { get; set; }

        [FieldDefinition(6, Label = "Require Approved", Type = FieldType.Checkbox, HelpText = "Require staff-approval for releases to be accepted.")]
        public bool RequireApproved { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
