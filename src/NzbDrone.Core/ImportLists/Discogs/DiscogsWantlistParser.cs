using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Discogs;

public class DiscogsWantlistParser : IParseImportListResponse
{
    private ImportListResponse _importListResponse;
    private IHttpClient _httpClient;
    private DiscogsWantlistSettings _settings;

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
        _importListResponse = importListResponse;

        var items = new List<ImportListItemInfo>();

        if (!PreProcess(_importListResponse))
        {
            return items;
        }

        var jsonResponse = Json.Deserialize<DiscogsWantlistResponse>(_importListResponse.Content);

        if (jsonResponse?.Wants == null)
        {
            return items;
        }

        foreach (var want in jsonResponse.Wants)
        {
            if (want?.BasicInformation != null)
            {
                try
                {
                    if (_httpClient != null && _settings != null)
                    {
                        var releaseInfo = FetchReleaseDetails(want.BasicInformation.ResourceUrl);
                        if (releaseInfo != null)
                        {
                            items.Add(releaseInfo);
                        }
                    }
                }
                catch
                {
                    // If we can't fetch release details, skip this item
                    continue;
                }
            }
        }

        return items;
    }

    // Unfortunately discogs release details are nested in a given /release/N endpoint.
    // We'll have to fetch each one to get proper details.
    private ImportListItemInfo FetchReleaseDetails(string resourceUrl)
    {
        var request = new HttpRequestBuilder(resourceUrl)
            .SetHeader("Authorization", $"Discogs token={_settings.Token}")
            .Build();

        var response = _httpClient.Execute(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var releaseResponse = Json.Deserialize<DiscogsReleaseResponse>(response.Content);

        if (releaseResponse?.Artists?.Any() == true && releaseResponse.Title.IsNotNullOrWhiteSpace())
        {
            return new ImportListItemInfo
            {
                Artist = releaseResponse.Artists.First().Name,
                Album = releaseResponse.Title
            };
        }

        return null;
    }

    protected virtual bool PreProcess(ImportListResponse importListResponse)
    {
        if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new ImportListException(importListResponse,
                "Discogs API call resulted in an unexpected StatusCode [{0}]",
                importListResponse.HttpResponse.StatusCode);
        }

        if (importListResponse.HttpResponse.Headers.ContentType != null &&
            importListResponse.HttpResponse.Headers.ContentType.Contains("text/html"))
        {
            throw new ImportListException(importListResponse,
                "Discogs API responded with HTML content. Wantlist may be too large or API may be unavailable.");
        }

        return true;
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
