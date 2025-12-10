using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Discogs;

public class DiscogsListsParser : IParseImportListResponse
{
    private IHttpClient _httpClient;
    private DiscogsListsSettings _settings;

    public DiscogsListsParser()
    {
    }

    public void SetContext(IHttpClient httpClient, DiscogsListsSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
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
        catch
        {
            // If we can't fetch release details, skip this item
            return null;
        }
    }
}

public class DiscogsListResponse
{
    public List<DiscogsListItem> Items { get; set; }
}

public class DiscogsListItem
{
    public string Type { get; set; }
    public int Id { get; set; }
    [JsonProperty("display_title")]
    public string DisplayTitle { get; set; }
    [JsonProperty("resource_url")]
    public string ResourceUrl { get; set; }
    public string Uri { get; set; }
}

public class DiscogsReleaseResponse
{
    public string Title { get; set; }
    public List<DiscogsReleaseArtist> Artists { get; set; }
}

public class DiscogsReleaseArtist
{
    public string Name { get; set; }
    public int Id { get; set; }
}
