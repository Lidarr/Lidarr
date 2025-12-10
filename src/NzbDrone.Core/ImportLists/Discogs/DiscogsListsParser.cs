using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Discogs;

public class DiscogsListsParser(DiscogsListsSettings settings, IHttpClient httpClient, Logger logger) : IParseImportListResponse
{
    private readonly DiscogsListsSettings _settings = settings;
    private readonly IHttpClient _httpClient = httpClient;
    private readonly Logger _logger = logger;

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
            if (item.ResourceUrl.IsNullOrWhiteSpace())
            {
                continue;
            }

            try
            {
                ImportListItemInfo itemInfo = null;

                if (item.Type == "release")
                {
                    itemInfo = FetchReleaseDetails(item.ResourceUrl);
                }
                else if (item.Type == "artist")
                {
                    itemInfo = FetchArtistDetails(item.ResourceUrl);
                }

                items.AddIfNotNull(itemInfo);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Discogs API call resulted in an unexpected exception for {0} type item", item.Type ?? "unknown");
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

    private ImportListItemInfo FetchArtistDetails(string resourceUrl)
    {
        return DiscogsParserHelper.FetchArtistDetails(_httpClient, _settings.Token, resourceUrl);
    }
}
