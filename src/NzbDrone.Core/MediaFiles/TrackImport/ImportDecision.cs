using NzbDrone.Common.Extensions;
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
        public IList<Rejection> Rejections { get; private set; }

        public bool Approved => Rejections.Empty();

        public ImportDecision(LocalTrack localTrack, params Rejection[] rejections)
        {
            LocalTrack = localTrack;
            Rejections = rejections.ToList();
        }

        public void Reject(Rejection rejection)
        {
            Rejections.Add(rejection);
        }
    }
}
