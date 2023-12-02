using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.LastFm
{
    public class LastFmSettingsValidator : AbstractValidator<LastFmUserSettings>
    {
        public LastFmSettingsValidator()
        {
            RuleFor(c => c.UserId).NotEmpty();
            RuleFor(c => c.Count).LessThanOrEqualTo(1000);
        }
    }

    public class LastFmUserSettings : IImportListSettings
    {
        private static readonly LastFmSettingsValidator Validator = new ();

        public LastFmUserSettings()
        {
            BaseUrl = "https://ws.audioscrobbler.com/2.0/";
            ApiKey = "204c76646d6020eee36bbc51a2fcd810";
            Method = (int)LastFmUserMethodList.TopArtists;
            Period = (int)LastFmUserTimePeriod.Overall;
            Count = 25;
        }

        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }

        [FieldDefinition(0, Label = "Last.fm UserID", HelpText = "Last.fm UserId to pull artists from")]
        public string UserId { get; set; }

        [FieldDefinition(1, Label = "List", Type = FieldType.Select, SelectOptions = typeof(LastFmUserMethodList))]
        public int Method { get; set; }

        [FieldDefinition(2, Label = "Period", Type = FieldType.Select, SelectOptions = typeof(LastFmUserTimePeriod), HelpText = "The time period over which to retrieve top artists for")]
        public int Period { get; set; }

        [FieldDefinition(3, Label = "Count", HelpText = "Number of results to pull from list (Max 1000)", Type = FieldType.Number)]
        public int Count { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum LastFmUserMethodList
    {
        [FieldOption(Label = "Top Artists")]
        TopArtists = 0,
        [FieldOption(Label = "Top Albums")]
        TopAlbums = 1
    }

    public enum LastFmUserTimePeriod
    {
        [FieldOption(Label = "Overall")]
        Overall = 0,
        [FieldOption(Label = "Last Week")]
        LastWeek = 1,
        [FieldOption(Label = "Last Month")]
        LastMonth = 2,
        [FieldOption(Label = "Last 3 Months")]
        LastThreeMonths = 3,
        [FieldOption(Label = "Last 6 Months")]
        LastSixMonths = 4,
        [FieldOption(Label = "Last 12 Months")]
        LastTwelveMonths = 5
    }
}
