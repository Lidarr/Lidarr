using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Deemix
{
    public class DeemixIndexerSettingsValidator : AbstractValidator<DeemixIndexerSettings>
    {
        public DeemixIndexerSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
        }
    }

    public class DeemixIndexerSettings : IIndexerSettings
    {
        private static readonly DeemixIndexerSettingsValidator Validator = new DeemixIndexerSettingsValidator();

        public DeemixIndexerSettings()
        {
            BaseUrl = "http://localhost:6595";
        }

        [FieldDefinition(0, Label = "URL", HelpText = "The URL to your Deemix download client")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Type = FieldType.Number, Label = "Early Download Limit", Unit = "days", HelpText = "Time before release date Lidarr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
