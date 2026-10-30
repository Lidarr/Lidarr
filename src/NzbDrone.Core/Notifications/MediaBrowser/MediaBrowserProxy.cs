using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Notifications.MediaBrowser.Model;

namespace NzbDrone.Core.Notifications.Emby
{
    public class MediaBrowserProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public MediaBrowserProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public void TestConnection(MediaBrowserSettings settings)
        {
            var path = "/System/Configuration";
            var request = BuildRequest(path, settings).Build();

            var response = _httpClient.Get(request);
            _logger.Trace("Response: {0}", response.Content);
        }

        public void Notify(MediaBrowserSettings settings, string title, string message)
        {
            var path = "/Notifications/Admin";
            var request = BuildRequest(path, settings).Build();
            request.Headers.ContentType = "application/json";

            request.SetContent(new
            {
                Name = title,
                Description = message,
                ImageUrl = "https://raw.github.com/lidarr/Lidarr/develop/Logo/64.png"
            }.ToJson());

            ProcessRequest(request);
        }

        public void Update(MediaBrowserSettings settings, List<string> musicCollectionPaths)
        {
            HttpRequest request;

            if (musicCollectionPaths.Any())
            {
                var path = "/Library/Media/Updated";
                request = BuildRequest(path, settings).Build();
                request.Headers.ContentType = "application/json";

                var updateInfo = new List<EmbyMediaUpdateInfo>();

                foreach (var colPath in musicCollectionPaths)
                {
                    updateInfo.Add(new EmbyMediaUpdateInfo
                    {
                        Path = colPath,
                        UpdateType = "Created"
                    });
                }

                request.SetContent(new
                {
                    Updates = updateInfo
                }.ToJson());
            }
            else
            {
                var path = "/Library/Refresh";
                request = BuildRequest(path, settings).Build();
            }

            ProcessRequest(request);
        }

        public List<EmbyMediaFolder> GetArtist(MediaBrowserSettings settings)
        {
            var path = "/Library/MediaFolders";
            var request = BuildRequest(path, settings).Build();

            return ProcessGetRequest<EmbyMediaFoldersResponse>(request).Items;
        }

        private T ProcessGetRequest<T>(HttpRequest request)
            where T : new()
        {
            var response = _httpClient.Get<T>(request);
            _logger.Trace("Response: {0}", response.Content);

            CheckForError(response);

            return response.Resource;
        }

        private string ProcessRequest(HttpRequest request)
        {
            var response = _httpClient.Post(request);
            _logger.Trace("Response: {0}", response.Content);

            CheckForError(response);

            return response.Content;
        }

        private string GetUrl(MediaBrowserSettings settings)
        {
            var scheme = settings.UseSsl ? "https" : "http";
            return $@"{scheme}://{settings.Address}";
        }

        private HttpRequestBuilder BuildRequest(string path, MediaBrowserSettings settings)
        {
            var url = GetUrl(settings);
            var request = new HttpRequestBuilder(url).Resource(path);

            request.Headers.Add("X-MediaBrowser-Token", settings.ApiKey);
            request.Headers.Add("Authorization", $"MediaBrowser Token=\"{settings.ApiKey}\"");

            return request;
        }

        private void CheckForError(HttpResponse response)
        {
            _logger.Debug("Looking for error in response: {0}", response);

            // TODO: actually check for the error
        }
    }
}
