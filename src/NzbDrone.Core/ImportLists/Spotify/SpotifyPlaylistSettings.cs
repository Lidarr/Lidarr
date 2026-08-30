using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyPlaylistSettingsValidator : SpotifySettingsBaseValidator<SpotifyPlaylistSettings>
    {
        public SpotifyPlaylistSettingsValidator()
        : base()
        {
            RuleFor(c => c.PlaylistIds).NotEmpty();
        }
    }

    public class SpotifyPlaylistSettings : SpotifySettingsBase<SpotifyPlaylistSettings>
    {
        protected override AbstractValidator<SpotifyPlaylistSettings> Validator => new SpotifyPlaylistSettingsValidator();

        public SpotifyPlaylistSettings()
        {
            PlaylistIds = System.Array.Empty<string>();
        }

        public override string Scope => "playlist-read-private user-library-read";

        [FieldDefinition(1, Label = "Playlists", Type = FieldType.Playlist)]
        public IEnumerable<string> PlaylistIds { get; set; }
    }
}
