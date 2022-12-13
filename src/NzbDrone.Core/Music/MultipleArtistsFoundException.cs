using System.Collections.Generic;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Music
{
    public class MultipleArtistsFoundException : NzbDroneException
    {
        public List<Artist> Artists { get; }

        public MultipleArtistsFoundException(List<Artist> artists, string message, params object[] args)
            : base(message, args)
        {
            Artists = artists;
        }
    }
}
