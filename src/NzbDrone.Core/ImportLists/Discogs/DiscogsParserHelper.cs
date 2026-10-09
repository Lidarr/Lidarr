using System.Linq;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Discogs;

internal static class DiscogsParserHelper
{
    public static ImportListItemInfo FetchReleaseDetails(IHttpClient httpClient, string token, string resourceUrl)
    {
        var request = new HttpRequestBuilder(resourceUrl)
            .SetHeader("Authorization", $"Discogs token={token}")
            .Build();

        var response = httpClient.Execute(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var releaseResponse = Json.Deserialize<DiscogsReleaseResponse>(response.Content);

        if (releaseResponse?.Artists?.Any() != true || releaseResponse.Title.IsNullOrWhiteSpace())
        {
            return null;
        }

        return new ImportListItemInfo
        {
            Artist = releaseResponse.Artists.First().Name,
            Album = releaseResponse.Title
        };
    }

    public static ImportListItemInfo FetchArtistDetails(IHttpClient httpClient, string token, string resourceUrl)
    {
        var request = new HttpRequestBuilder(resourceUrl)
            .SetHeader("Authorization", $"Discogs token={token}")
            .Build();

        var response = httpClient.Execute(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var artistResponse = Json.Deserialize<DiscogsArtistResponse>(response.Content);

        if (artistResponse?.Name.IsNullOrWhiteSpace() == true)
        {
            return null;
        }

        return new ImportListItemInfo
        {
            Artist = artistResponse.Name,
            Album = null // Artists don't have a specific album, just the artist name
        };
    }

    public static void EnsureValidResponse(ImportListResponse importListResponse, string htmlContentMessage)
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
            throw new ImportListException(importListResponse, htmlContentMessage);
        }
    }
}
