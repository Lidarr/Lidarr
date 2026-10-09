using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Discogs
{
    public class DiscogsWantlistSettingsValidator : AbstractValidator<DiscogsWantlistSettings>
    {
        public DiscogsWantlistSettingsValidator()
        {
            RuleFor(c => c.Token).NotEmpty();
            RuleFor(c => c.Username).NotEmpty();
        }
    }

    public class DiscogsWantlistSettings : IImportListSettings
    {
        private static readonly DiscogsWantlistSettingsValidator Validator = new DiscogsWantlistSettingsValidator();

        public DiscogsWantlistSettings()
        {
            BaseUrl = "https://api.discogs.com";
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "Token", Privacy = PrivacyLevel.ApiKey, HelpText = "Discogs Personal Access Token")]
        public string Token { get; set; }

        [FieldDefinition(1, Label = "Username", HelpText = "Discogs username whose wantlist to import")]
        public string Username { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
