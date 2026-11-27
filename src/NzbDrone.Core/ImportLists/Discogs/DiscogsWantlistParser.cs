using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Discogs;

public class DiscogsWantlistParser : IParseImportListResponse
{
    private readonly DiscogsWantlistSettings _settings;
    private readonly IHttpClient _httpClient;

    public DiscogsWantlistParser(DiscogsWantlistSettings settings, IHttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
    }

    public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
    {
        var items = new List<ImportListItemInfo>();

        if (!PreProcess(importListResponse))
        {
            return items;
        }

        var jsonResponse = Json.Deserialize<DiscogsWantlistResponse>(importListResponse.Content);

        if (jsonResponse?.Wants == null)
        {
            return items;
        }

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

            items.AddIfNotNull(new ImportListItemInfo
            {
                Artist = basicInfo.Artists.First().Name,
                Album = basicInfo.Title
            });
        }

        return items;
    }

    private bool PreProcess(ImportListResponse importListResponse)
    {
        DiscogsParserHelper.EnsureValidResponse(importListResponse,
            "Discogs API responded with HTML content. Wantlist may be too large or API may be unavailable.");
        return true;
    }
}
