using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Transmission
{
    public class TransmissionSettingsValidator : AbstractValidator<TransmissionSettings>
    {
        public TransmissionSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);

            RuleFor(c => c.UrlBase).ValidUrlBase();

            RuleFor(c => c.MusicCategory).Matches(@"^\.?[-a-z]*$", RegexOptions.IgnoreCase).WithMessage("Allowed characters a-z and -");

            RuleFor(c => c.MusicCategory).Empty()
                .When(c => c.MusicDirectory.IsNotNullOrWhiteSpace())
                .WithMessage("Cannot use Category and Directory");
        }
    }

    public class TransmissionSettings : IProviderConfig
    {
        private static readonly TransmissionSettingsValidator Validator = new TransmissionSettingsValidator();

        // This constructor is used when creating a new instance, such as the user adding a new Transmission client.
        public TransmissionSettings()
        {
            Host = "localhost";
            Port = 9091;
            UrlBase = "/transmission/";
            MusicCategory = "lidarr";
        }

        // TODO: Remove this in v3
        // This constructor is used when deserializing from JSON, it will set the
        // category to the deserialized value, defaulting to null.
        [JsonConstructor]
        public TransmissionSettings(string musicCategory = null)
        {
            Host = "localhost";
            Port = 9091;
            UrlBase = "/transmission/";
            MusicCategory = musicCategory;
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Use SSL", Type = FieldType.Checkbox, HelpText = "Use secure connection when connecting to Transmission")]
        public bool UseSsl { get; set; }

        [FieldDefinition(3, Label = "Url Base", Type = FieldType.Textbox, Advanced = true, HelpText = "Adds a prefix to the transmission rpc url, eg http://[host]:[port]/[urlBase]/rpc, defaults to '/transmission/'")]
        public string UrlBase { get; set; }

        [FieldDefinition(4, Label = "Username", Type = FieldType.Textbox, Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(5, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(6, Label = "Category", Type = FieldType.Textbox, HelpText = "Adding a category specific to Lidarr avoids conflicts with unrelated downloads, but it's optional. Creates a [category] subdirectory in the output directory.")]
        public string MusicCategory { get; set; }

        [FieldDefinition(7, Label = "PostImportCategory", Type = FieldType.Textbox, Advanced = true, HelpText = "DownloadClientSettingsPostImportCategoryHelpText")]
        public string MusicImportedCategory { get; set; }

        [FieldDefinition(8, Label = "Directory", Type = FieldType.Textbox, Advanced = true, HelpText = "Optional location to put downloads in, leave blank to use the default Transmission location")]
        public string MusicDirectory { get; set; }

        [FieldDefinition(9, Label = "Recent Priority", Type = FieldType.Select, SelectOptions = typeof(TransmissionPriority), HelpText = "Priority to use when grabbing albums released within the last 14 days")]
        public int RecentMusicPriority { get; set; }

        [FieldDefinition(10, Label = "Older Priority", Type = FieldType.Select, SelectOptions = typeof(TransmissionPriority), HelpText = "Priority to use when grabbing albums released over 14 days ago")]
        public int OlderMusicPriority { get; set; }

        [FieldDefinition(11, Label = "Add Paused", Type = FieldType.Checkbox)]
        public bool AddPaused { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
