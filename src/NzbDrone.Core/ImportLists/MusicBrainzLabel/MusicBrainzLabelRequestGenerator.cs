using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.MusicBrainzLabel
{
    public class MusicBrainzLabelRequestGenerator : IImportListRequestGenerator
    {
        // MusicBrainz caps browse requests at 100 entities per page.
        public const int PageSize = 100;

        public MusicBrainzLabelSettings Settings { get; set; }

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public MusicBrainzLabelRequestGenerator(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public virtual ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetPagedRequests());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetPagedRequests()
        {
            var labelId = MusicBrainzLabelSettings.ParseLabelId(Settings.LabelId);

            if (labelId == null)
            {
                _logger.Warn("'{0}' is not a MusicBrainz label ID", Settings.LabelId);
                yield break;
            }

            // The parser filters by release type, so a page of 100 releases can
            // come back as a handful of albums — and the base class stops paging
            // as soon as a page looks short. Ask MusicBrainz for the real release
            // count up front and emit exactly the pages we need, so filtering can
            // never truncate the list. MusicBrainzLabel.IsFullPage is overridden
            // to match.
            var available = GetReleaseCount(labelId);
            var total = Math.Min(available, Settings.MaxReleases);

            if (available > Settings.MaxReleases)
            {
                _logger.Warn("Label {0} has {1} releases, only scanning the first {2}. Raise 'Maximum Releases' to see the rest.",
                    labelId,
                    available,
                    Settings.MaxReleases);
            }
            else
            {
                _logger.Debug("Label {0} has {1} releases", labelId, available);
            }

            for (var offset = 0; offset < total; offset += PageSize)
            {
                yield return new ImportListRequest(BuildRequest(labelId, offset, Math.Min(PageSize, total - offset)));
            }
        }

        private int GetReleaseCount(string labelId)
        {
            // A limit=1 browse is the cheapest way to read release-count.
            var response = _httpClient.Execute(BuildRequest(labelId, 0, 1));

            var parsed = JsonConvert.DeserializeObject<MusicBrainzLabelBrowseResponse>(response.Content);

            return parsed?.ReleaseCount ?? 0;
        }

        private HttpRequest BuildRequest(string labelId, int offset, int limit)
        {
            return new HttpRequestBuilder(Settings.BaseUrl.TrimEnd('/'))
                .Resource("/ws/2/release")
                .AddQueryParam("label", labelId)
                .AddQueryParam("inc", "release-groups+artist-credits")
                .AddQueryParam("fmt", "json")
                .AddQueryParam("limit", limit)
                .AddQueryParam("offset", offset)
                .Build();
        }
    }
}
