﻿using System;
using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class UpgradeSpecification : IImportDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public UpgradeSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalTrack localTrack)
        {
            var qualityComparer = new QualityModelComparer(localTrack.Artist.Profile);
            if (localTrack.Tracks.Any(e => e.TrackFileId != 0 && qualityComparer.Compare(e.TrackFile.Value.Quality, localTrack.Quality) > 0))
            {
                _logger.Debug("This file isn't an upgrade for all tracks. Skipping {0}", localTrack.Path);
                return Decision.Reject("Not an upgrade for existing track file(s)");
            }

            return Decision.Accept();
        }

        public Decision IsSatisfiedBy(LocalEpisode localEpisode)
        {
            var qualityComparer = new QualityModelComparer(localEpisode.Series.Profile);
            if (localEpisode.Episodes.Any(e => e.EpisodeFileId != 0 && qualityComparer.Compare(e.EpisodeFile.Value.Quality, localEpisode.Quality) > 0))
            {
                _logger.Debug("This file isn't an upgrade for all episodes. Skipping {0}", localEpisode.Path);
                return Decision.Reject("Not an upgrade for existing episode file(s)");
            }

            return Decision.Accept();
        }
    }
}
