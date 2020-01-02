﻿using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class TrackFileDeletedEvent : IEvent
    {
        public TrackFile TrackFile { get; private set; }
        public DeleteMediaFileReason Reason { get; private set; }

        public TrackFileDeletedEvent(TrackFile trackFile, DeleteMediaFileReason reason)
        {
            TrackFile = trackFile;
            Reason = reason;
        }
    }
}
