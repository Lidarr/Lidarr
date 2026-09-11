using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.MusicBrainzLabel
{
    // Values are persisted in the database, so they must stay stable. Append
    // new members rather than renumbering.
    public enum MusicBrainzPrimaryType
    {
        [FieldOption(Label = "Album")]
        Album = 0,

        [FieldOption(Label = "EP")]
        Ep = 1,

        [FieldOption(Label = "Single")]
        Single = 2,

        [FieldOption(Label = "Broadcast")]
        Broadcast = 3,

        [FieldOption(Label = "Other")]
        Other = 4
    }

    public enum MusicBrainzSecondaryType
    {
        [FieldOption(Label = "Compilation")]
        Compilation = 0,

        [FieldOption(Label = "Live")]
        Live = 1,

        [FieldOption(Label = "Remix")]
        Remix = 2,

        [FieldOption(Label = "Soundtrack")]
        Soundtrack = 3,

        [FieldOption(Label = "DJ-mix")]
        DjMix = 4,

        [FieldOption(Label = "Mixtape/Street")]
        MixtapeStreet = 5,

        [FieldOption(Label = "Demo")]
        Demo = 6,

        [FieldOption(Label = "Interview")]
        Interview = 7,

        [FieldOption(Label = "Spokenword")]
        Spokenword = 8,

        [FieldOption(Label = "Audiobook")]
        Audiobook = 9,

        [FieldOption(Label = "Audio drama")]
        AudioDrama = 10,

        [FieldOption(Label = "Field recording")]
        FieldRecording = 11
    }

    public class MusicBrainzLabelSettingsValidator : AbstractValidator<MusicBrainzLabelSettings>
    {
        public MusicBrainzLabelSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).NotEmpty();

            RuleFor(c => c.LabelId)
                .NotEmpty()
                .Must(id => MusicBrainzLabelSettings.ParseLabelId(id) != null)
                .WithMessage("Must be a MusicBrainz label ID (a GUID) or the URL of a label page");

            RuleFor(c => c.PrimaryTypes)
                .NotEmpty()
                .WithMessage("At least one release type must be selected");

            RuleFor(c => c.MaxReleases).GreaterThan(0);

            RuleFor(c => c.MinimumYear)
                .InclusiveBetween(1000, 2999)
                .When(c => c.MinimumYear.HasValue);
        }
    }

    public class MusicBrainzLabelSettings : IImportListSettings
    {
        private static readonly MusicBrainzLabelSettingsValidator Validator = new MusicBrainzLabelSettingsValidator();

        private static readonly Regex GuidRegex = new Regex(
            @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public MusicBrainzLabelSettings()
        {
            BaseUrl = "https://musicbrainz.org";

            // Albums and EPs, minus the reissue/compilation noise that makes a
            // label list balloon. Singles are off by default because on most
            // labels they outnumber everything else combined.
            PrimaryTypes = new[]
            {
                (int)MusicBrainzPrimaryType.Album,
                (int)MusicBrainzPrimaryType.Ep
            };

            ExcludedSecondaryTypes = new[]
            {
                (int)MusicBrainzSecondaryType.Compilation,
                (int)MusicBrainzSecondaryType.Live,
                (int)MusicBrainzSecondaryType.Remix,
                (int)MusicBrainzSecondaryType.DjMix
            };

            OfficialReleasesOnly = true;
            ExcludeVariousArtists = true;
            MaxReleases = 500;
        }

        [FieldDefinition(0, Label = "MusicBrainzLabelSettingsUrl", Advanced = true, HelpText = "MusicBrainzLabelSettingsUrlHelpText")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "MusicBrainzLabelSettingsLabelId", HelpText = "MusicBrainzLabelSettingsLabelIdHelpText", HelpLink = "https://musicbrainz.org/search?type=label")]
        public string LabelId { get; set; }

        [FieldDefinition(2, Label = "MusicBrainzLabelSettingsReleaseTypes", Type = FieldType.Select, SelectOptions = typeof(MusicBrainzPrimaryType), HelpText = "MusicBrainzLabelSettingsReleaseTypesHelpText")]
        public IEnumerable<int> PrimaryTypes { get; set; }

        [FieldDefinition(3, Label = "MusicBrainzLabelSettingsExcludeSecondaryTypes", Type = FieldType.Select, SelectOptions = typeof(MusicBrainzSecondaryType), HelpText = "MusicBrainzLabelSettingsExcludeSecondaryTypesHelpText")]
        public IEnumerable<int> ExcludedSecondaryTypes { get; set; }

        [FieldDefinition(4, Label = "MusicBrainzLabelSettingsOfficialReleasesOnly", Type = FieldType.Checkbox, HelpText = "MusicBrainzLabelSettingsOfficialReleasesOnlyHelpText")]
        public bool OfficialReleasesOnly { get; set; }

        [FieldDefinition(5, Label = "MusicBrainzLabelSettingsExcludeVariousArtists", Type = FieldType.Checkbox, HelpText = "MusicBrainzLabelSettingsExcludeVariousArtistsHelpText")]
        public bool ExcludeVariousArtists { get; set; }

        [FieldDefinition(6, Label = "MusicBrainzLabelSettingsMinimumYear", Type = FieldType.Number, HelpText = "MusicBrainzLabelSettingsMinimumYearHelpText")]
        public int? MinimumYear { get; set; }

        [FieldDefinition(7, Label = "MusicBrainzLabelSettingsMaxReleases", Type = FieldType.Number, Advanced = true, HelpText = "MusicBrainzLabelSettingsMaxReleasesHelpText")]
        public int MaxReleases { get; set; }

        /// <summary>
        /// Accepts a bare GUID or any MusicBrainz label URL and returns the GUID,
        /// or null if there isn't one.
        /// </summary>
        public static string ParseLabelId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var match = GuidRegex.Match(value);

            return match.Success ? match.Value.ToLowerInvariant() : null;
        }

        public HashSet<string> GetPrimaryTypeNames()
        {
            return BuildNameSet(PrimaryTypes, value => MusicBrainzTypeNames.Primary((MusicBrainzPrimaryType)value));
        }

        public HashSet<string> GetExcludedSecondaryTypeNames()
        {
            return BuildNameSet(ExcludedSecondaryTypes, value => MusicBrainzTypeNames.Secondary((MusicBrainzSecondaryType)value));
        }

        private static HashSet<string> BuildNameSet(IEnumerable<int> values, Func<int, string> map)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in (values ?? Enumerable.Empty<int>()).Select(map))
            {
                if (name != null)
                {
                    set.Add(name);
                }
            }

            return set;
        }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    /// <summary>
    /// Maps our enum members onto the exact strings MusicBrainz uses in its JSON.
    /// </summary>
    internal static class MusicBrainzTypeNames
    {
        public static string Primary(MusicBrainzPrimaryType type)
        {
            return type switch
            {
                MusicBrainzPrimaryType.Album => "Album",
                MusicBrainzPrimaryType.Ep => "EP",
                MusicBrainzPrimaryType.Single => "Single",
                MusicBrainzPrimaryType.Broadcast => "Broadcast",
                MusicBrainzPrimaryType.Other => "Other",
                _ => null
            };
        }

        public static string Secondary(MusicBrainzSecondaryType type)
        {
            return type switch
            {
                MusicBrainzSecondaryType.Compilation => "Compilation",
                MusicBrainzSecondaryType.Live => "Live",
                MusicBrainzSecondaryType.Remix => "Remix",
                MusicBrainzSecondaryType.Soundtrack => "Soundtrack",
                MusicBrainzSecondaryType.DjMix => "DJ-mix",
                MusicBrainzSecondaryType.MixtapeStreet => "Mixtape/Street",
                MusicBrainzSecondaryType.Demo => "Demo",
                MusicBrainzSecondaryType.Interview => "Interview",
                MusicBrainzSecondaryType.Spokenword => "Spokenword",
                MusicBrainzSecondaryType.Audiobook => "Audiobook",
                MusicBrainzSecondaryType.AudioDrama => "Audio drama",
                MusicBrainzSecondaryType.FieldRecording => "Field recording",
                _ => null
            };
        }
    }
}
