using System;
using System.Linq;

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

            return AlbumYearMatchResult.Reject(yearDiff, $"Release year {parsedYear.Value} does not match album year {albumYear} {yearDiff}");
        }

        public AlbumYearMatchResult Match(Album album, int? parsedYear)
        {
            if (!parsedYear.HasValue)
            {
                return AlbumYearMatchResult.NoYearProvided();
            }

            var primaryResult = Match(album.ReleaseDate, parsedYear);
            if (primaryResult.IsMatch && primaryResult.YearDifference <= AlbumYearMatchingOptions.ExactMatchYearTolerance)
            {
                return primaryResult;
            }

            // Check album releases for remasters/editions with different years
            if (album.AlbumReleases != null && album.AlbumReleases.IsLoaded)
            {
                var releases = album.AlbumReleases.Value;
                if (releases != null && releases.Any())
                {
                    foreach (var release in releases.Where(r => r.Monitored || album.AnyReleaseOk))
                    {
                        var releaseResult = Match(release.ReleaseDate, parsedYear);
                        if (releaseResult.IsMatch &&
                            releaseResult.YearDifference <= AlbumYearMatchingOptions.ExactMatchYearTolerance)
                        {
                            return releaseResult;
                        }
                    }
                }
            }

            // Be more lenient for compilations and live albums
            if (album.SecondaryTypes != null && album.SecondaryTypes.Any(t => t.Name == "Compilation" || t.Name == "Live"))
            {
                if (primaryResult.YearDifference <= AlbumYearMatchingOptions.HardRejectYearDiff)
                {
                    return AlbumYearMatchResult.Match(primaryResult.YearDifference ?? 0, -AlbumYearMatchingOptions.YearPenaltyPerYear, YearMatchConfidence.Low);
                }
            }

            return primaryResult;
        }

        public double CalculateYearScore(DateTime? albumReleaseDate, int? expectedYear)
        {
            var result = Match(albumReleaseDate, expectedYear);
            return result.ScoreAdjustment;
        }
    }
}
