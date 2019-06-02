using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.ImportLists.Exceptions
{
    public class ImportListTokenException : NzbDroneException
    {
        public ImportListTokenException(string message, params object[] args)
            : base(message, args)
        { }
    }
}
