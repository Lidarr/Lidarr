using System;

namespace NzbDrone.Common.Disk
{
    [Flags]
    public enum TransferMode
    {
        None = 0,

        Move = 1,
        Copy = 2,
        HardLink = 4,
        RefLink = 8,

        HardLinkOrCopy = Copy | HardLink,
    }
}
