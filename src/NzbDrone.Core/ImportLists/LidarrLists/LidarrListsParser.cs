using Newtonsoft.Json;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.Parser.Model;
using System.Collections.Generic;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.ImportLists.LidarrLists
{
    public class LidarrListsParser : IParseImportListResponse
    {
        private readonly LidarrListsSettings _settings;
        private ImportListResponse _importListResponse;
        private readonly Logger _logger;

        public LidarrListsParser(LidarrListsSettings settings)
        {
            _settings = settings;
        }

        public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
        {
            _importListResponse = importListResponse;

            var movies = new List<ImportListItemInfo>();

            if (!PreProcess(_importListResponse))
            {
                return movies;
            }

            var jsonResponse = JsonConvert.DeserializeObject<List<LidarrListsAlbum>>(_importListResponse.Content);

            // no albums were return
            if (jsonResponse == null)
            {
                return movies;
            }

            foreach (var item in jsonResponse)
            {
                movies.AddIfNotNull(new ImportListItemInfo
                {
                    Artist = item.ArtistName,
                    Album = item.AlbumTitle,
                    ArtistMusicBrainzId = item.ArtistId,
                    AlbumMusicBrainzId = item.AlbumId,
                    ReleaseDate = item.ReleaseDate.GetValueOrDefault()
                });
            }

            return movies;
        }

        protected virtual bool PreProcess(ImportListResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(indexerResponse, "Import List API call resulted in an unexpected StatusCode [{0}]", indexerResponse.HttpResponse.StatusCode);
            }

            if (indexerResponse.HttpResponse.Headers.ContentType != null && indexerResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                indexerResponse.HttpRequest.Headers.Accept != null && !indexerResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(indexerResponse, "Import List responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }

    }
}
