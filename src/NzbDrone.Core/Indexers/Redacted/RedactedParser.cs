using System.Collections.Generic;
using System.Linq;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Redacted
{
    public class RedactedParser : IParseIndexerResponse
    {
        private readonly RedactedSettings _settings;

        public RedactedParser(RedactedSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<GazelleResponse>(indexerResponse.HttpResponse);
            if (jsonResponse.Resource.Status != "success" ||
                jsonResponse.Resource.Status.IsNullOrWhiteSpace() ||
                jsonResponse.Resource.Response == null)
            {
                return torrentInfos;
            }

            foreach (var result in jsonResponse.Resource.Response.Results)
            {
                if (result.Torrents != null)
                {
                    foreach (var torrent in result.Torrents)
                    {
                        var id = torrent.TorrentId;
                        var artist = WebUtility.HtmlDecode(result.Artist);
                        var album = WebUtility.HtmlDecode(result.GroupName);

                        var title = $"{result.Artist} - {result.GroupName} ({result.GroupYear}) [{torrent.Format} {torrent.Encoding}] [{torrent.Media}]";
                        if (torrent.HasCue)
                        {
                            title += " [Cue]";
                        }

                        torrentInfos.Add(new GazelleInfo()
                        {
                            Guid = string.Format("Redacted-{0}", id),
                            Artist = artist,

                            // Splice Title from info to avoid calling API again for every torrent.
                            Title = WebUtility.HtmlDecode(title),
                            Album = album,
                            Container = torrent.Encoding,
                            Codec = torrent.Format,
                            Size = long.Parse(torrent.Size),
                            DownloadUrl = GetDownloadUrl(id),
                            InfoUrl = GetInfoUrl(result.GroupId, id),
                            Seeders = int.Parse(torrent.Seeders),
                            Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                            PublishDate = torrent.Time.ToUniversalTime(),
                            Scene = torrent.Scene,
                        });
                    }
                }
            }

            // order by date
            return
                torrentInfos
                    .OrderByDescending(o => o.PublishDate)
                    .ToArray();
        }

        private string GetDownloadUrl(int torrentId)
        {
            // AuthKey is required but not checked, just pass in a dummy variable
            // to avoid having to track authkey, which is randomly cycled
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId)
                .AddQueryParam("authkey", "lidarr")
                .AddQueryParam("torrent_pass", _settings.PassKey)
                .AddQueryParam("usetoken", _settings.UseFreeleechToken ? 1 : 0);

            return url.FullUri;
        }

        private string GetInfoUrl(string groupId, int torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("id", groupId)
                .AddQueryParam("torrentid", torrentId);

            return url.FullUri;
        }
    }
}
