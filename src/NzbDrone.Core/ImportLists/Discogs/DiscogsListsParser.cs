using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Discogs;

public class DiscogsListsParser : IParseImportListResponse
{
    private IHttpClient _httpClient;
    private DiscogsListsSettings _settings;
    private Logger _logger;

    public DiscogsListsParser()
    {
    }

    public void SetContext(IHttpClient httpClient, DiscogsListsSettings settings, Logger logger = null)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger ?? LogManager.GetCurrentClassLogger();
    }

    public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
    {
        DiscogsParserHelper.EnsureValidResponse(importListResponse,
            "Discogs API responded with HTML content. List may be too large or API may be unavailable.");

        var jsonResponse = Json.Deserialize<DiscogsListResponse>(importListResponse.Content);

        if (jsonResponse?.Items == null)
        {
            return new List<ImportListItemInfo>();
        }

        var items = new List<ImportListItemInfo>();

        foreach (var resourceUrl in jsonResponse.Items.Where(IsReleaseItem).Select(item => item.ResourceUrl))
        {
            var releaseInfo = TryFetchRelease(resourceUrl);

            if (releaseInfo != null)
            {
                items.Add(releaseInfo);
            }
        }

        return items;
    }

    private static bool IsReleaseItem(DiscogsListItem item)
    {
        return item?.Type == "release" && item.ResourceUrl.IsNotNullOrWhiteSpace();
    }

    private ImportListItemInfo TryFetchRelease(string resourceUrl)
    {
        if (_httpClient == null || _settings == null)
        {
            return null;
        }

        try
        {
            return DiscogsParserHelper.FetchReleaseDetails(_httpClient, _settings.Token, resourceUrl);
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to fetch release details from Discogs API for resource URL: {0}. Skipping this item.", resourceUrl);
            return null;
        }
    }
}
