using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Indexers.Waffles
{
    public class WafflesException : NzbDroneException
    {
        public WafflesException(string message, params object[] args) : base(message, args)
        {
        }

        public WafflesException(string message) : base(message)
        {
        }
    }
}
