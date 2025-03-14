using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.Youtube
{
    public class YoutubePlaylistSettingsValidator : YoutubeSettingsBaseValidator<YoutubePlaylistSettings>
    {
        public YoutubePlaylistSettingsValidator()
        : base()
        {
            RuleFor(c => c.PlaylistIds).NotEmpty();
        }
    }

    public class YoutubePlaylistSettings : YoutubeSettingsBase<YoutubePlaylistSettings>
    {
        protected override AbstractValidator<YoutubePlaylistSettings> Validator =>
            new YoutubePlaylistSettingsValidator();

        public YoutubePlaylistSettings()
        {
            PlaylistIds = System.Array.Empty<string>();
        }

        // public override string Scope => "playlist-read-private";

        [FieldDefinition(1, Label = "Playlists", Type = FieldType.Textbox)]
        public IEnumerable<string> PlaylistIds { get; set; }
    }
}
