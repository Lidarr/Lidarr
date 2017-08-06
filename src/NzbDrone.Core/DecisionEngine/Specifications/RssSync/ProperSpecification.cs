﻿using System;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class ProperSpecification : IDecisionEngineSpecification
    {
        private readonly QualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public ProperSpecification(QualityUpgradableSpecification qualityUpgradableSpecification, IConfigService configService, Logger logger)
        {
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _configService = configService;
            _logger = logger;
        }

        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            throw new NotImplementedException();

            // TODO Rework to associate TrackFiles to Album

            //if (searchCriteria != null)
            //{
            //    return Decision.Accept();
            //}

            //foreach (var file in subject.Episodes.Where(c => c.EpisodeFileId != 0).Select(c => c.EpisodeFile.Value))
            //{
            //    if (_qualityUpgradableSpecification.IsRevisionUpgrade(file.Quality, subject.ParsedEpisodeInfo.Quality))
            //    {
            //        if (file.DateAdded < DateTime.Today.AddDays(-7))
            //        {
            //            _logger.Debug("Proper for old file, rejecting: {0}", subject);
            //            return Decision.Reject("Proper for old file");
            //        }

            //        if (!_configService.AutoDownloadPropers)
            //        {
            //            _logger.Debug("Auto downloading of propers is disabled");
            //            return Decision.Reject("Proper downloading is disabled");
            //        }
            //    }
            //}

            //return Decision.Accept();
        }
    }
}
