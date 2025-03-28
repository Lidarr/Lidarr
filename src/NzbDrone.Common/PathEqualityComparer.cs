using System;
using System.Collections.Generic;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common
{
    public class PathEqualityComparer : IEqualityComparer<string>
    {
        public static readonly PathEqualityComparer Instance = new ();

        private PathEqualityComparer()
        {
        }

        public bool Equals(string x, string y)
        {
            return x.PathEquals(y);
        }

        public int GetHashCode(string obj)
        {
            try
            {
                if (OsInfo.IsWindows)
                {
                    return obj.CleanFilePath().Normalize().ToLower().GetHashCode();
                }

                return obj.CleanFilePath().Normalize().GetHashCode();
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Invalid path: {obj}", ex);
            }
        }
    }
}
