using System;

namespace NzbDrone.Core.MediaFiles
{
    public interface ITrackFileFilter
    {
        Func<string, bool> GetExclusionPredicate(string basePath);
    }
}
