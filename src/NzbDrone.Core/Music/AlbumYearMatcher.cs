using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Music
{
    public class AlbumYearMatcher : IAlbumYearMatcher
    {
        public AlbumYearMatchResult Match(DateTime? albumReleaseDate, int? parsedYear)
        {
            if (!parsedYear.HasValue)
            {
                return AlbumYearMatchResult.NoYearProvided();
            }

            if (!albumReleaseDate.HasValue)
            {
                return AlbumYearMatchResult.Match(0, 0, YearMatchConfidence.Low);
            }

            var albumYear = albumReleaseDate.Value.Year;
            var yearDiff = Math.Abs(albumYear - parsedYear.Value);

            if (yearDiff == 0)
            {
                return AlbumYearMatchResult.Match(0, AlbumYearMatchingOptions.ExactYearBonus, YearMatchConfidence.High);
            }

            if (yearDiff <= AlbumYearMatchingOptions.ExactMatchYearTolerance)
            {
                return AlbumYearMatchResult.Match(yearDiff, AlbumYearMatchingOptions.CloseYearBonus, YearMatchConfidence.High);
            }

            if (yearDiff <= AlbumYearMatchingOptions.FuzzyMatchMaxYearDiff)
            {
                var penalty = -AlbumYearMatchingOptions.YearPenaltyPerYear * (yearDiff - AlbumYearMatchingOptions.ExactMatchYearTolerance);
                return AlbumYearMatchResult.Match(yearDiff, penalty, YearMatchConfidence.Medium);
            }

            if (yearDiff <= AlbumYearMatchingOptions.HardRejectYearDiff)
            {
                var penalty = -AlbumYearMatchingOptions.YearPenaltyPerYear * (yearDiff - AlbumYearMatchingOptions.ExactMatchYearTolerance);
                return AlbumYearMatchResult.Match(yearDiff, penalty, YearMatchConfidence.Low);
            }

            return AlbumYearMatchResult.Reject(yearDiff, $"Release year {parsedYear.Value} does not match album year {albumYear} ({yearDiff} years apart)");
        }

        public AlbumYearMatchResult Match(Album album, int? parsedYear)
        {
            if (!parsedYear.HasValue)
            {
                return AlbumYearMatchResult.NoYearProvided();
            }

            if (TitleContainsYear(album.Title, parsedYear.Value))
            {
                return AlbumYearMatchResult.Match(0, AlbumYearMatchingOptions.ExactYearBonus, YearMatchConfidence.High);
            }

            var knownDates = new List<DateTime>();

            if (album.ReleaseDate.HasValue)
            {
                knownDates.Add(album.ReleaseDate.Value);
            }

            // Check album releases for remasters/editions with different years
            if (album.AlbumReleases != null && album.AlbumReleases.IsLoaded && album.AlbumReleases.Value != null)
            {
                knownDates.AddRange(album.AlbumReleases.Value.Where(r => r.ReleaseDate.HasValue).Select(r => r.ReleaseDate.Value));
            }

            var primaryResult = knownDates.Any()
                ? knownDates.Select(d => Match(d, parsedYear))
                    .OrderByDescending(r => r.IsMatch)
                    .ThenByDescending(r => r.ScoreAdjustment)
                    .ThenBy(r => r.YearDifference)
                    .First()
                : Match((DateTime?)null, parsedYear);

            // Be more lenient for compilations and live albums
            if (album.SecondaryTypes != null && album.SecondaryTypes.Any(t => t.Name == "Compilation" || t.Name == "Live"))
            {
                if (primaryResult.YearDifference > AlbumYearMatchingOptions.ExactMatchYearTolerance)
                {
                    return AlbumYearMatchResult.Match(primaryResult.YearDifference ?? 0, primaryResult.ScoreAdjustment, YearMatchConfidence.Low);
                }
            }

            return primaryResult;
        }

        public double CalculateYearScore(DateTime? albumReleaseDate, int? expectedYear)
        {
            var result = Match(albumReleaseDate, expectedYear);
            return result.ScoreAdjustment;
        }

        public double CalculateAlbumYearScore(Album album, int? expectedYear)
        {
            return Match(album, expectedYear).ScoreAdjustment;
        }

        public double CalculateOriginalYearScore(Album album, int? expectedYear)
        {
            return CalculateYearScore(album.ReleaseDate, expectedYear);
        }

        public static bool TitleContainsYear(string title, int year)
        {
            return !string.IsNullOrEmpty(title) && Regex.IsMatch(title, $@"(?<!\d){year}(?!\d)");
        }
    }
}
