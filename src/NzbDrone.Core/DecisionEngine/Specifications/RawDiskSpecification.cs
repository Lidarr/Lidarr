﻿using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class RawDiskSpecification : IDecisionEngineSpecification
    {
        private static readonly string[] _cdContainerTypes = new[] { "vob", "iso" };

        private readonly Logger _logger;

        public RawDiskSpecification(Logger logger)
        {
            _logger = logger;
        }

        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.Release == null || subject.Release.Container.IsNullOrWhiteSpace())
            {
                return Decision.Accept();
            }

                if (_cdContainerTypes.Contains(subject.Release.Container.ToLower()))
                {
                    _logger.Debug("Release contains raw CD, rejecting.");
                    return Decision.Reject("Raw CD release");
                }

            return Decision.Accept();
        }
    }
}
