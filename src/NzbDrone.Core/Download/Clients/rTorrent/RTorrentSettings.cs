using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.RTorrent
{
    public class RTorrentSettingsValidator : AbstractValidator<RTorrentSettings>
    {
        public RTorrentSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);
            RuleFor(c => c.MusicCategory).NotEmpty()
                                      .WithMessage("A category is recommended")
                                      .AsWarning();
        }
    }

    public class RTorrentSettings : IProviderConfig
    {
        private static readonly RTorrentSettingsValidator Validator = new RTorrentSettingsValidator();

        public RTorrentSettings()
        {
            Host = "localhost";
            Port = 8080;
            UrlBase = "RPC2";
            MusicCategory = "lidarr";
            OlderMusicPriority = (int)RTorrentPriority.Normal;
            RecentMusicPriority = (int)RTorrentPriority.Normal;
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Use SSL", Type = FieldType.Checkbox, HelpText = "Use secure connection when connecting to rTorrent")]
        public bool UseSsl { get; set; }

        [FieldDefinition(3, Label = "Url Path", Type = FieldType.Textbox, HelpText = "Path to the XMLRPC endpoint, see http(s)://[host]:[port]/[urlPath]. This is usually RPC2 or [path to ruTorrent]/plugins/rpc/rpc.php when using ruTorrent.")]
        public string UrlBase { get; set; }

        [FieldDefinition(4, Label = "Username", Type = FieldType.Textbox, Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(5, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(6, Label = "Category", Type = FieldType.Textbox, HelpText = "Adding a category specific to Lidarr avoids conflicts with unrelated non-Lidarr downloads. Using a category is optional, but strongly recommended.")]
        public string MusicCategory { get; set; }

        [FieldDefinition(7, Label = "PostImportCategory", Type = FieldType.Textbox, Advanced = true, HelpText = "DownloadClientSettingsPostImportCategoryHelpText")]
        public string MusicImportedCategory { get; set; }

        [FieldDefinition(8, Label = "Directory", Type = FieldType.Textbox, Advanced = true, HelpText = "Optional location to put downloads in, leave blank to use the default rTorrent location")]
        public string MusicDirectory { get; set; }

        [FieldDefinition(9, Label = "DownloadClientSettingsRecentPriority", Type = FieldType.Select, SelectOptions = typeof(RTorrentPriority), HelpText = "DownloadClientSettingsRecentPriorityAlbumHelpText")]
        public int RecentMusicPriority { get; set; }

        [FieldDefinition(10, Label = "DownloadClientSettingsOlderPriority", Type = FieldType.Select, SelectOptions = typeof(RTorrentPriority), HelpText = "DownloadClientSettingsOlderPriorityAlbumHelpText")]
        public int OlderMusicPriority { get; set; }

        [FieldDefinition(11, Label = "Add Stopped", Type = FieldType.Checkbox, HelpText = "Enabling will add torrents and magnets to rTorrent in a stopped state. This may break magnet files.")]
        public bool AddStopped { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
