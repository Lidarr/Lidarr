using System;
using System.Collections.Generic;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists
{
    public abstract class HttpImportListWithExpiringTokenBase<TSettings, TToken> : HttpImportListBase<TSettings>
        where TSettings : IImportListSettings, new()
    {
        public HttpImportListWithExpiringTokenBase(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        { }

        public abstract ImportListRequestGeneratorWithExpiringTokenBase<TToken> GetRequestGeneratorWithExpiringToken();
        public abstract TToken ParseResponseForToken(HttpResponse tokenResponse);

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return GetRequestGeneratorWithExpiringToken();
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            return FetchReleases(g => g.GetListItems(), true);
        }

        protected virtual IList<ImportListItemInfo> FetchReleases(Func<ImportListRequestGeneratorWithExpiringTokenBase<TToken>, ImportListPageableRequestChain> pageableRequestChainSelector, bool isRecent = false)
        {
            var generator = GetRequestGeneratorWithExpiringToken();
            GetAuthorizationToken(generator);

            var pageableRequestChain = pageableRequestChainSelector(generator);
            return FetchReleases(pageableRequestChain);
        }

        private void GetAuthorizationToken(ImportListRequestGeneratorWithExpiringTokenBase<TToken> generator)
        {
            var refreshTokenRequest = generator.GetRefreshTokenRequest();

            try
            {
                var tokenResponse = _httpClient.Execute(refreshTokenRequest);
                var token = ParseResponseForToken(tokenResponse);
                generator.Token = token;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.NameResolutionFailure ||
                    webException.Status == WebExceptionStatus.ConnectFailure)
                {
                    _importListStatusService.RecordConnectionFailure(Definition.Id);
                }
                else
                {
                    _importListStatusService.RecordFailure(Definition.Id);
                }

                if (webException.Message.Contains("502") || webException.Message.Contains("503") ||
                    webException.Message.Contains("timed out"))
                {
                    _logger.Warn("{0} server is currently unavailable. {1} {2}", this, refreshTokenRequest.Url, webException.Message);
                }
                else
                {
                    _logger.Warn("{0} {1} {2}", this, refreshTokenRequest.Url, webException.Message);
                }
            }
            catch (TooManyRequestsException ex)
            {
                if (ex.RetryAfter != TimeSpan.Zero)
                {
                    _importListStatusService.RecordFailure(Definition.Id, ex.RetryAfter);
                }
                else
                {
                    _importListStatusService.RecordFailure(Definition.Id, TimeSpan.FromHours(1));
                }
                _logger.Warn("API Request Limit reached for {0}", this);
            }
            catch (HttpException ex)
            {
                _importListStatusService.RecordFailure(Definition.Id);
                _logger.Warn("{0} {1}", this, ex.Message);
            }
            catch (Exception ex)
            {
                _importListStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", refreshTokenRequest.Url);
                _logger.Error(ex, "An error occurred while processing feed. {0}", refreshTokenRequest.Url);
            }
        }
    }

}
