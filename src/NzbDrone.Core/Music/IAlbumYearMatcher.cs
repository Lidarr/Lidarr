using System;

namespace NzbDrone.Core.Music
{
    public enum YearMatchConfidence
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public class AlbumYearMatchResult
    {
        public bool IsMatch { get; set; }
        public int? YearDifference { get; set; }
        public double ScoreAdjustment { get; set; }
        public string RejectionReason { get; set; }
        public YearMatchConfidence Confidence { get; set; }

        public static AlbumYearMatchResult NoYearProvided() => new AlbumYearMatchResult
        {
            IsMatch = true,
            YearDifference = null,
            ScoreAdjustment = 0,
            Confidence = YearMatchConfidence.None
        };

        public static AlbumYearMatchResult Match(int yearDiff, double scoreAdjustment, YearMatchConfidence confidence) => new AlbumYearMatchResult
        {
            IsMatch = true,
            YearDifference = yearDiff,
            ScoreAdjustment = scoreAdjustment,
            Confidence = confidence
        };

        public static AlbumYearMatchResult Reject(int yearDiff, string reason) => new AlbumYearMatchResult
        {
            IsMatch = false,
            YearDifference = yearDiff,
            ScoreAdjustment = -AlbumYearMatchingOptions.YearPenaltyPerYear * AlbumYearMatchingOptions.HardRejectYearDiff,
            RejectionReason = reason,
            Confidence = YearMatchConfidence.High
        };
    }

    public interface IAlbumYearMatcher
    {
        AlbumYearMatchResult Match(DateTime? albumReleaseDate, int? parsedYear);
        AlbumYearMatchResult Match(Album album, int? parsedYear);
        double CalculateYearScore(DateTime? albumReleaseDate, int? expectedYear);
    }
}
