using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.MusicBrainzSeries;

public class MusicBrainzSeriesSettingsValidator : AbstractValidator<MusicBrainzSeriesSettings>
{
    public MusicBrainzSeriesSettingsValidator()
    {
    }
}

public class MusicBrainzSeriesSettings : IImportListSettings
{
    private static readonly MusicBrainzSeriesSettingsValidator Validator = new MusicBrainzSeriesSettingsValidator();

    public MusicBrainzSeriesSettings()
    {
        BaseUrl = "";
    }

    public string BaseUrl { get; set; }

    [FieldDefinition(0, Label = "Series Id", HelpText = "The GUID at the end of the MusicBrainz URL (e.g. 4b5f2897-9b05-4799-b895-6620e27143e7)")]
    public string SeriesId { get; set; }

    public NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
