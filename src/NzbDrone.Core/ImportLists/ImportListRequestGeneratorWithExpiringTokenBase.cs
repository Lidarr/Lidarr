using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists.Exceptions;

namespace NzbDrone.Core.ImportLists
{
    public abstract class ImportListRequestGeneratorWithExpiringTokenBase<TToken> : IImportListRequestGenerator
    {
        public TToken Token { protected get; set; }

        public abstract HttpRequest GetRefreshTokenRequest();

        public abstract ImportListPageableRequestChain GetListItemsWithExpiringToken();

        public ImportListPageableRequestChain GetListItems()
        {
            if (Token == null)
            {
                throw new ImportListTokenException("The token needs to be set before generating requests that need the Authorization token");
            }
            return GetListItemsWithExpiringToken();
        }

        protected abstract ImportListRequest AddTokenToRequest(ImportListRequest request);
    }
}
