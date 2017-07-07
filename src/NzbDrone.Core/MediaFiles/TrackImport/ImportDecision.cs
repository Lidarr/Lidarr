﻿using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.MediaFiles.TrackImport
{
    public class ImportDecision
    {
        public LocalTrack LocalTrack { get; private set; }
        public IEnumerable<Rejection> Rejections { get; private set; }

        public bool Approved => Rejections.Empty();

        public object LocalEpisode { get; internal set; }

        public ImportDecision(LocalTrack localTrack, params Rejection[] rejections)
        {
            LocalTrack = localTrack;
            Rejections = rejections.ToList();
        }
    }
}
