using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Rarbg
{
    public class RarbgSettingsValidator : AbstractValidator<RarbgSettings>
    {
        public RarbgSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();

            RuleFor(c => c.SeedCriteria).SetValidator(_ => new SeedCriteriaSettingsValidator());
        }
    }

    public class RarbgSettings : ITorrentIndexerSettings
    {
        private static readonly RarbgSettingsValidator Validator = new RarbgSettingsValidator();

        public RarbgSettings()
        {
            BaseUrl = "https://torrentapi.org";
            RankedOnly = false;
            MinimumSeeders = IndexerDefaults.MINIMUM_SEEDERS;
        }

        [FieldDefinition(0, Label = "API URL", HelpText = "URL to Rarbg api, not the website.")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Type = FieldType.Checkbox, Label = "Ranked Only", HelpText = "Only include ranked results.")]
        public bool RankedOnly { get; set; }
        
        [FieldDefinition(2, Type = FieldType.Captcha, Label = "CAPTCHA Token", HelpText = "CAPTCHA Clearance token used to handle CloudFlare Anti-DDOS measures on shared-ip VPNs.")]
        public string CaptchaToken { get; set; }

        [FieldDefinition(3, Type = FieldType.Textbox, Label = "Minimum Seeders", HelpText = "Minimum number of seeders required.", Advanced = true)]
        public int MinimumSeeders { get; set; }

        [FieldDefinition(4)]
        public SeedCriteriaSettings SeedCriteria { get; } = new SeedCriteriaSettings();

        [FieldDefinition(5, Type = FieldType.Number, Label = "Early Download Limit", HelpText = "Time before release date Lidarr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
