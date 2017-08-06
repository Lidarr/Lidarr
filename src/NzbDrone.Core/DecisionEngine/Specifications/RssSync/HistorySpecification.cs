﻿using System;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.History;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class HistorySpecification : IDecisionEngineSpecification
    {
        private readonly IHistoryService _historyService;
        private readonly QualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public HistorySpecification(IHistoryService historyService,
                                           QualityUpgradableSpecification qualityUpgradableSpecification,
                                           IConfigService configService,
                                           Logger logger)
        {
            _historyService = historyService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _configService = configService;
            _logger = logger;
        }

        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                _logger.Debug("Skipping history check during search");
                return Decision.Accept();
            }

            var cdhEnabled = _configService.EnableCompletedDownloadHandling;

            _logger.Debug("Performing history status check on report");
            foreach (var album in subject.Albums)
            {
                _logger.Debug("Checking current status of album [{0}] in history", album.Id);
                var mostRecent = _historyService.MostRecentForAlbum(album.Id);

                if (mostRecent != null && mostRecent.EventType == HistoryEventType.Grabbed)
                {
                    var recent = mostRecent.Date.After(DateTime.UtcNow.AddHours(-12));
                    var cutoffUnmet = _qualityUpgradableSpecification.CutoffNotMet(subject.Artist.Profile, mostRecent.Quality, subject.ParsedAlbumInfo.Quality);
                    var upgradeable = _qualityUpgradableSpecification.IsUpgradable(subject.Artist.Profile, mostRecent.Quality, subject.ParsedAlbumInfo.Quality);

                    if (!recent && cdhEnabled)
                    {
                        continue;
                    }

                    if (!cutoffUnmet)
                    {
                        if (recent)
                        {
                            return Decision.Reject("Recent grab event in history already meets cutoff: {0}", mostRecent.Quality);
                        }

                        return Decision.Reject("CDH is disabled and grab event in history already meets cutoff: {0}", mostRecent.Quality);
                    }

                    if (!upgradeable)
                    {
                        if (recent)
                        {
                            return Decision.Reject("Recent grab event in history is of equal or higher quality: {0}", mostRecent.Quality);
                        }

                        return Decision.Reject("CDH is disabled and grab event in history is of equal or higher quality: {0}", mostRecent.Quality);
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
