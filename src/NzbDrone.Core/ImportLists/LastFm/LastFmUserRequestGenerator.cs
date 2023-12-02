using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.LastFm
{
    public class LastFmUserRequestGenerator : IImportListRequestGenerator
    {
        public LastFmUserSettings Settings { get; set; }

        public int MaxPages { get; set; }
        public int PageSize { get; set; }

        public LastFmUserRequestGenerator()
        {
            MaxPages = 1;
            PageSize = 1000;
        }

        public virtual ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetPagedRequests());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetPagedRequests()
        {
            var method = Settings.Method switch
            {
                (int)LastFmUserMethodList.TopAlbums => "user.gettopalbums",
                _ => "user.gettopartists"
            };

            var period = Settings.Period switch
            {
                (int)LastFmUserTimePeriod.LastWeek => "7day",
                (int)LastFmUserTimePeriod.LastMonth => "1month",
                (int)LastFmUserTimePeriod.LastThreeMonths => "3month",
                (int)LastFmUserTimePeriod.LastSixMonths => "6month",
                (int)LastFmUserTimePeriod.LastTwelveMonths => "12month",
                _ => "overall"
            };

            var request = new HttpRequestBuilder(Settings.BaseUrl)
                .AddQueryParam("api_key", Settings.ApiKey)
                .AddQueryParam("method", method)
                .AddQueryParam("user", Settings.UserId)
                .AddQueryParam("period", period)
                .AddQueryParam("limit", Settings.Count)
                .AddQueryParam("format", "json")
                .Accept(HttpAccept.Json)
                .Build();

            yield return new ImportListRequest(request);
        }
    }
}
