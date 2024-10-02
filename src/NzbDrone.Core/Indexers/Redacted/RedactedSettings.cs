using System.Collections.Generic;
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
            Categories = new[]
            {
                (int)RedactedCategory.Music
            };
            MinimumSeeders = IndexerDefaults.MINIMUM_SEEDERS;
        }

        [FieldDefinition(0, Label = "IndexerSettingsApiUrl", Advanced = true, HelpTextWarning = "IndexerSettingsApiUrlHelpText")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "ApiKey", HelpText = "Generate this in 'Access Settings' in your Redacted profile", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(2, Label = "Categories", Type = FieldType.Select, SelectOptions = typeof(RedactedCategory), HelpText = "If unspecified, all options are used.")]
        public IEnumerable<int> Categories { get; set; }

        [FieldDefinition(3, Type = FieldType.Checkbox, Label = "Use Freeleech Token", HelpText = "Will cause grabbing to fail if you do not have any tokens available", Advanced = true)]
        public bool UseFreeleechToken { get; set; }

        [FieldDefinition(4, Type = FieldType.Number, Label = "Early Download Limit", Unit = "days", HelpText = "Time before release date Lidarr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        [FieldDefinition(5, Type = FieldType.Textbox, Label = "Minimum Seeders", HelpText = "Minimum number of seeders required.", Advanced = true)]
        public int MinimumSeeders { get; set; }

        [FieldDefinition(6)]
        public SeedCriteriaSettings SeedCriteria { get; set; } = new ();

        [FieldDefinition(7, Type = FieldType.Checkbox, Label = "IndexerSettingsRejectBlocklistedTorrentHashes", HelpText = "IndexerSettingsRejectBlocklistedTorrentHashesHelpText", Advanced = true)]
        public bool RejectBlocklistedTorrentHashesWhileGrabbing { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum RedactedCategory
    {
        [FieldOption(label: "Music")]
        Music = 1,
        [FieldOption(label: "Applications")]
        Applications = 2,
        [FieldOption(label: "E-Books")]
        EBooks = 3,
        [FieldOption(label: "Audiobooks")]
        Audiobooks = 4,
        [FieldOption(label: "E-Learning Videos")]
        ELearningVideos = 5,
        [FieldOption(label: "Comedy")]
        Comedy = 6,
        [FieldOption(label: "Comics")]
        Comics = 7
    }
}
