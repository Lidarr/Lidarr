using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Discogs;

public class DiscogsListsParser : IParseImportListResponse
{
    private readonly DiscogsListsSettings _settings;
    private readonly IHttpClient _httpClient;
    private readonly Logger _logger;

    public DiscogsListsParser(DiscogsListsSettings settings, IHttpClient httpClient, Logger logger)
    {
        _settings = settings;
        _httpClient = httpClient;
        _logger = logger;
    }

    public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
    {
        var items = new List<ImportListItemInfo>();

        if (!PreProcess(importListResponse))
        {
            return items;
        }

        var jsonResponse = Json.Deserialize<DiscogsListResponse>(importListResponse.Content);

        if (jsonResponse?.Items == null)
        {
            return items;
        }

        foreach (var item in jsonResponse.Items)
        {
            if (item.Type == "release" && item.ResourceUrl.IsNotNullOrWhiteSpace())
            {
                try
                {
                    var releaseInfo = FetchReleaseDetails(item.ResourceUrl);
                    items.AddIfNotNull(releaseInfo);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Discogs release details API call resulted in an unexpected exception");
                }
            }
        }

        return items;
    }

    private bool PreProcess(ImportListResponse importListResponse)
    {
        DiscogsParserHelper.EnsureValidResponse(importListResponse,
            "Discogs API responded with HTML content. List may be too large or API may be unavailable.");
        return true;
    }

    private ImportListItemInfo FetchReleaseDetails(string resourceUrl)
    {
        return DiscogsParserHelper.FetchReleaseDetails(_httpClient, _settings.Token, resourceUrl);
    }
}
