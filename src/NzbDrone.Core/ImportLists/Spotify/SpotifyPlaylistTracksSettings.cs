using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyPlaylistTracksSettingsValidator : SpotifySettingsBaseValidator<SpotifyPlaylistTracksSettings>
    {
        public SpotifyPlaylistTracksSettingsValidator()
            : base()
        {
            RuleFor(c => c.PlaylistId).NotEmpty();
            RuleFor(c => c.Count).LessThanOrEqualTo(10000);
        }
    }

    public class SpotifyPlaylistTracksSettings : SpotifySettingsBase<SpotifyPlaylistTracksSettings>
    {
        protected override AbstractValidator<SpotifyPlaylistTracksSettings> Validator => new SpotifyPlaylistTracksSettingsValidator();

        public SpotifyPlaylistTracksSettings()
        {
            Count = 10000;
        }

        [FieldDefinition(0, Label = "Spotify Playlist ID", HelpText = "Playlist ID to pull artists from. Can be obtained by copying a playlist URL and keeping only the last part.")]
        public string PlaylistId { get; set; }

        [FieldDefinition(1, Label = "Count", HelpText = "Number of results to pull from list (Max 10000)", Type = FieldType.Number)]
        public int Count { get; set; }
    }
}
