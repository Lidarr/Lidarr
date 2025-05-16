using System;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Youtube
{
    public class YoutubeSettingsBaseValidator<TSettings> : AbstractValidator<TSettings>
        where TSettings : YoutubeSettingsBase<TSettings>
    {
        public YoutubeSettingsBaseValidator()
        {
            // TODO
        }
    }

    public class YoutubeSettingsBase<TSettings> : IImportListSettings
        where TSettings : YoutubeSettingsBase<TSettings>
    {
        protected virtual AbstractValidator<TSettings> Validator => new YoutubeSettingsBaseValidator<TSettings>();

        public string BaseUrl { get; set; }

        public virtual string Scope => "";

        [FieldDefinition(0, Label = "Access Token", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public string AccessToken { get; set; }

        [FieldDefinition(0, Label = "Refresh Token", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public string RefreshToken { get; set; }

        [FieldDefinition(0, Label = "Expires", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public DateTime Expires { get; set; }

        // [FieldDefinition(99, Label = "Authenticate with Google", Type = FieldType.OAuth)]
        // public string SignIn { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate((TSettings)this));
        }
    }
}
