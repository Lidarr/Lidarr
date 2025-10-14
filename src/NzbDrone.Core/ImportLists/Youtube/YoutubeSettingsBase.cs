using FluentValidation;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Youtube
{
    public class YoutubeSettingsBaseValidator<TSettings> : AbstractValidator<TSettings>
        where TSettings : YoutubeSettingsBase<TSettings>
    {
    }

    public class YoutubeSettingsBase<TSettings> : IImportListSettings
        where TSettings : YoutubeSettingsBase<TSettings>
    {
        protected virtual AbstractValidator<TSettings> Validator => new YoutubeSettingsBaseValidator<TSettings>();

        public string BaseUrl { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate((TSettings)this));
        }
    }
}
