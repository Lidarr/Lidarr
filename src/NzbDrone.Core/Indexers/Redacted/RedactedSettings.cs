using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Redacted
{
    public class RedactedSettingsValidator : AbstractValidator<RedactedSettings>
    {
        public RedactedSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class RedactedSettings : ITorrentIndexerSettings
    {
        private static readonly RedactedSettingsValidator Validator = new ();

        public RedactedSettings()
        {
            BaseUrl = "https://redacted.ch";
            MinimumSeeders = IndexerDefaults.MINIMUM_SEEDERS;
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "ApiKey", HelpText = "Generate this in 'Access Settings' in your Redacted profile", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(2, Type = FieldType.Textbox, Label = "Minimum Seeders", HelpText = "Minimum number of seeders required.", Advanced = true)]
        public int MinimumSeeders { get; set; }

        [FieldDefinition(3)]
        public SeedCriteriaSettings SeedCriteria { get; set; } = new ();

        [FieldDefinition(4, Type = FieldType.Number, Label = "Early Download Limit", Unit = "days", HelpText = "Time before release date Lidarr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        [FieldDefinition(5, Type = FieldType.Checkbox, Label = "Use Freeleech Token", HelpText = "Will cause grabbing to fail if you do not have any tokens available", Advanced = true)]
        public bool UseFreeleechToken { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
