using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Deluge
{
    public class DelugeSettingsValidator : AbstractValidator<DelugeSettings>
    {
        public DelugeSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);

            RuleFor(c => c.MusicCategory).Matches("^[-a-z0-9]*$").WithMessage("Allowed characters a-z, 0-9 and -");
            RuleFor(c => c.MusicImportedCategory).Matches("^[-a-z0-9]*$").WithMessage("Allowed characters a-z, 0-9 and -");
        }
    }

    public class DelugeSettings : IProviderConfig
    {
        private static readonly DelugeSettingsValidator Validator = new DelugeSettingsValidator();

        public DelugeSettings()
        {
            Host = "localhost";
            Port = 8112;
            Password = "deluge";
            MusicCategory = "lidarr";
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Url Base", Type = FieldType.Textbox, Advanced = true, HelpText = "Adds a prefix to the deluge json url, see http://[host]:[port]/[urlBase]/json")]
        public string UrlBase { get; set; }

        [FieldDefinition(3, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "Category", Type = FieldType.Textbox, HelpText = "Adding a category specific to Lidarr avoids conflicts with unrelated downloads, but it's optional")]
        public string MusicCategory { get; set; }

        [FieldDefinition(5, Label = "Post-Import Category", Type = FieldType.Textbox, Advanced = true, HelpText = "Category for Lidarr to set after it has imported the download. Lidarr will not remove torrents in that category even if seeding finished. Leave blank to keep same category.")]
        public string MusicImportedCategory { get; set; }

        [FieldDefinition(6, Label = "Recent Priority", Type = FieldType.Select, SelectOptions = typeof(DelugePriority), HelpText = "Priority to use when grabbing albums released within the last 14 days")]
        public int RecentTvPriority { get; set; }

        [FieldDefinition(7, Label = "Older Priority", Type = FieldType.Select, SelectOptions = typeof(DelugePriority), HelpText = "Priority to use when grabbing albums released over 14 days ago")]
        public int OlderTvPriority { get; set; }

        [FieldDefinition(8, Label = "Add Paused", Type = FieldType.Checkbox)]
        public bool AddPaused { get; set; }

        [FieldDefinition(9, Label = "Use SSL", Type = FieldType.Checkbox)]
        public bool UseSsl { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
