using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Discogs;

public class DiscogsWantlistParser : IParseImportListResponse
{
    public DiscogsWantlistParser()
    {
    }

    public void SetContext(IHttpClient httpClient, DiscogsWantlistSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
    {
        DiscogsParserHelper.EnsureValidResponse(importListResponse,
            "Discogs API responded with HTML content. Wantlist may be too large or API may be unavailable.");

        var jsonResponse = Json.Deserialize<DiscogsWantlistResponse>(importListResponse.Content);

        if (jsonResponse?.Wants == null)
        {
            return new List<ImportListItemInfo>();
        }

        var items = new List<ImportListItemInfo>();

        foreach (var want in jsonResponse.Wants)
        {
            var basicInfo = want?.BasicInformation;

            if (basicInfo == null)
            {
                continue;
            }

            // The wantlist API includes artists and title in basic_information, so no need to fetch release details
            // If you want is artists.First().Name and title, then fetching the release details is redundant according to their API.
            if (basicInfo.Artists?.Any() != true || basicInfo.Title.IsNullOrWhiteSpace())
            {
                continue;
            }

            items.Add(new ImportListItemInfo
            {
                Artist = basicInfo.Artists.First().Name,
                Album = basicInfo.Title
            });
        }

        return items;
    }
}

public class DiscogsWantlistResponse
{
    public List<DiscogsWantlistItem> Wants { get; set; }
}

public class DiscogsWantlistItem
{
    [JsonProperty("basic_information")]
    public DiscogsBasicInformation BasicInformation { get; set; }
}

public class DiscogsBasicInformation
{
    public int Id { get; set; }
    public string Title { get; set; }
    [JsonProperty("resource_url")]
    public string ResourceUrl { get; set; }
    public List<DiscogsReleaseArtist> Artists { get; set; }
}
