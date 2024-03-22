using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.MusicBrainzLabel
{
    public class MusicBrainzLabelSettingsValidator : AbstractValidator<MusicBrainzLabelSettings>
    {
    }

    public class MusicBrainzLabelSettings : IImportListSettings
    {
        private static readonly MusicBrainzLabelSettingsValidator Validator = new MusicBrainzLabelSettingsValidator();

        public MusicBrainzLabelSettings()
        {
            BaseUrl = "";
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "Label Id", HelpText = "The GUID at the end of the MusicBrainz URL (e.g. 4b5f2897-9b05-4799-b895-6620e27143e7)")]
        public string LabelId { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
