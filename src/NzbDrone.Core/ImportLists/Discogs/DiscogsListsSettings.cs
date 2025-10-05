using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Discogs
{
    public class DiscogsListsSettingsValidator : AbstractValidator<DiscogsListsSettings>
    {
        public DiscogsListsSettingsValidator()
        {
            RuleFor(c => c.Token).NotEmpty();
            RuleFor(c => c.ListId).NotEmpty();
        }
    }

    public class DiscogsListsSettings : IImportListSettings
    {
        private static readonly DiscogsListsSettingsValidator Validator = new DiscogsListsSettingsValidator();

        public DiscogsListsSettings()
        {
            BaseUrl = "https://api.discogs.com";
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "Token", Privacy = PrivacyLevel.ApiKey, HelpText = "Discogs Personal Access Token")]
        public string Token { get; set; }

        [FieldDefinition(1, Label = "List ID", HelpText = "ID of the Discogs list to import")]
        public string ListId { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
