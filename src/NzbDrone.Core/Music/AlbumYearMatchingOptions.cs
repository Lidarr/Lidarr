namespace NzbDrone.Core.Music
{
    public static class AlbumYearMatchingOptions
    {
        public const int ExactMatchYearTolerance = 1;
        public const int FuzzyMatchMaxYearDiff = 3;
        public const int HardRejectYearDiff = 5;

        public const double ExactYearBonus = 0.20;
        public const double CloseYearBonus = 0.10;
        public const double YearPenaltyPerYear = 0.05;

        public const double TitleMinThresholdWithYear = 0.55;
        public const double TitleMinThresholdNoYear = 0.70;
        public const double TitleFuzzThreshold = 0.70;
        public const double TitleFuzzGap = 0.40;
    }
}
