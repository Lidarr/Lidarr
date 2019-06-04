using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifySettingsBaseValidator<TSettings> : AbstractValidator<TSettings>
        where TSettings : SpotifySettingsBase<TSettings>
    {
        public SpotifySettingsBaseValidator()
        {
            RuleFor(c => c.ClientId).NotEmpty();
            RuleFor(c => c.ClientSecret).NotEmpty();
        }
    }

    public class SpotifySettingsBase<TSettings> : IImportListSettings
        where TSettings : SpotifySettingsBase<TSettings>
    {
        protected virtual AbstractValidator<TSettings> Validator => new SpotifySettingsBaseValidator<TSettings>();

        public SpotifySettingsBase()
        {
            BaseUrl = "https://api.spotify.com/v1";
            ClientId = "ec110b65c5a247ada633baeebfbeb0ba";
            ClientSecret = "b8dc353479614c878c5f8c439f7b05a2";
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "Spotify API Client ID", HelpText = "Specify your own Client ID if you prefer. Create an app on Spotify's API dashboard to get a Client ID and a Client Secret (https://developer.spotify.com/dashboard/applications). Default : Lidarr's Client ID")]
        public string ClientId { get; set; }

        [FieldDefinition(0, Label = "Spotify API Client Secret", HelpText = "Specify your own Client Secret if you specified your own Client ID. Create an app on Spotify's API dashboard to get a Client ID and a Client Secret (https://developer.spotify.com/dashboard/applications). Default : Lidarr's Client Secret")]
        public string ClientSecret { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate((TSettings)this));
        }
    }
}
