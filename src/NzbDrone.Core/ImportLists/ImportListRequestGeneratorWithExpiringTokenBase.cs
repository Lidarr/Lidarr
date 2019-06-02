using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists.Exceptions;

namespace NzbDrone.Core.ImportLists
{
    public abstract class ImportListRequestGeneratorWithExpiringTokenBase<TToken> : IImportListRequestGenerator
    {
        public TToken Token { private get; set; }

        public abstract HttpRequest GetRefreshTokenRequest();

        public abstract ImportListPageableRequestChain GetListItems(TToken token);

        public ImportListPageableRequestChain GetListItems()
        {
            if (Token == null)
            {
                throw new ImportListTokenException("The token needs to be set before generating requests that need the Authorization token");
            }
            return GetListItems(Token);
        }
    }
}
