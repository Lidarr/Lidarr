using NLog;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles.Languages;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public interface IUpgradableSpecification
    {
        bool IsUpgradable(QualityProfile profile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, int currentScore, QualityModel newQuality, Language newLanguage, int newScore);
        bool QualityCutoffNotMet(QualityProfile profile, QualityModel currentQuality, QualityModel newQuality = null);
        bool LanguageCutoffNotMet(LanguageProfile languageProfile, Language currentLanguage);
        bool CutoffNotMet(QualityProfile profile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, int currentScore, QualityModel newQuality = null, int newScore = 0);
        bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality);
    }

    public class UpgradableSpecification : IUpgradableSpecification
    {
        private readonly Logger _logger;

        public UpgradableSpecification(Logger logger)
        {
            _logger = logger;
        }

        private bool IsLanguageUpgradable(LanguageProfile profile, Language currentLanguage, Language newLanguage = null) 
        {
            if (newLanguage != null)
            {
                var compare = new LanguageComparer(profile).Compare(newLanguage, currentLanguage);
                if (compare <= 0)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsQualityUpgradable(QualityProfile profile, QualityModel currentQuality, QualityModel newQuality = null)
        {
            if (newQuality != null)
            {
                var compare = new QualityModelComparer(profile).Compare(newQuality, currentQuality);

                if (compare <= 0)
                {
                    _logger.Debug("Existing item has better quality, skipping");
                    return false;
                }
            }
            return true;
        }

        private bool IsPreferredWordUpgradable(int currentScore, int newScore)
        {
            return newScore > currentScore;
        }

        public bool IsUpgradable(QualityProfile qualityProfile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, int currentScore, QualityModel newQuality, Language newLanguage, int newScore)
        {
            if (IsQualityUpgradable(qualityProfile, currentQuality, newQuality) && qualityProfile.UpgradeAllowed)
            {
                return true;
            }

            if (new QualityModelComparer(qualityProfile).Compare(newQuality, currentQuality) < 0)
            {
                _logger.Debug("Existing item has better quality, skipping");
                return false;
            }

            if (IsLanguageUpgradable(languageProfile, currentLanguage, newLanguage) && languageProfile.UpgradeAllowed)
            {
                return true;
            }

            if (new LanguageComparer(languageProfile).Compare(newLanguage, currentLanguage) < 0)
            {
                _logger.Debug("Existing item has better language, skipping");
                return false;
            }

            if (!IsPreferredWordUpgradable(currentScore, newScore))
            {
                _logger.Debug("Existing item has a better preferred word score, skipping");
                return false;
            }

            if (!IsPreferredWordUpgradable(currentScore, newScore))
            {
                _logger.Debug("Existing item has a better preferred word score, skipping");
                return false;
            }

            return true;
        }

        public bool QualityCutoffNotMet(QualityProfile profile, QualityModel currentQuality, QualityModel newQuality = null)
        {
            var qualityCompare = new QualityModelComparer(profile).Compare(currentQuality.Quality.Id, profile.Cutoff);

            if (qualityCompare < 0)
            {
                return true;
            }

            if (qualityCompare == 0 && newQuality != null && IsRevisionUpgrade(currentQuality, newQuality))
            {
                return true;
            }

            return false;
        }

        public bool LanguageCutoffNotMet(LanguageProfile languageProfile, Language currentLanguage)
        {
            var languageCompare = new LanguageComparer(languageProfile).Compare(currentLanguage, languageProfile.Cutoff);

            return languageCompare < 0;
        }

        public bool CutoffNotMet(QualityProfile profile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, int currentScore, QualityModel newQuality = null, int newScore = 0)
        {
            // If we can upgrade the language (it is not the cutoff) then the quality doesn't
            // matter as we can always get same quality with prefered language.
            if (LanguageCutoffNotMet(languageProfile, currentLanguage))
            {
                return true;
            }

            if (QualityCutoffNotMet(profile, currentQuality, newQuality))
            {
                return true;
            }

            if (IsPreferredWordUpgradable(currentScore, newScore))
            {
                return true;
            }

            _logger.Debug("Existing item meets cut-off. skipping.");

            return false;
        }

        public bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality)
        {
            var compare = newQuality.Revision.CompareTo(currentQuality.Revision);

            // Comparing the quality directly because we don't want to upgrade to a proper for a webrip from a webdl or vice versa
            if (currentQuality.Quality == newQuality.Quality && compare > 0)
            {
                _logger.Debug("New quality is a better revision for existing quality");
                return true;
            }

            return false;
        }
    }
}
