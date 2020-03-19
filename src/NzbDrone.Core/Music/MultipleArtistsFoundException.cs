using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Music
{
    public class MultipleArtistsFoundException : NzbDroneException
    {
        public MultipleArtistsFoundException(string message, params object[] args)
            : base(message, args)
        {
        }

        public MultipleArtistsFoundException(string message)
            : base(message)
        {
        }

        public MultipleArtistsFoundException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        protected MultipleArtistsFoundException(string message, System.Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
