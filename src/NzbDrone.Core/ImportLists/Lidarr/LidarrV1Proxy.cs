using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Lidarr
{
    public interface ILidarrV1Proxy
    {
        List<LidarrArtist> GetArtists(LidarrSettings settings);
        List<LidarrAlbum> GetAlbums(LidarrSettings settings);
        List<LidarrProfile> GetProfiles(LidarrSettings settings);
        List<LidarrTag> GetTags(LidarrSettings settings);
        ValidationFailure Test(LidarrSettings settings);
    }

    public class LidarrV1Proxy : ILidarrV1Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public LidarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<LidarrArtist> GetArtists(LidarrSettings settings)
        {
            return Execute<LidarrArtist>("/api/v1/artist", settings);
        }

        public List<LidarrAlbum> GetAlbums(LidarrSettings settings)
        {
            return Execute<LidarrAlbum>("/api/v1/album", settings);
        }

        public List<LidarrProfile> GetProfiles(LidarrSettings settings)
        {
            return Execute<LidarrProfile>("/api/v1/qualityprofile", settings);
        }

        public List<LidarrTag> GetTags(LidarrSettings settings)
        {
            return Execute<LidarrTag>("/api/v1/tag", settings);
        }

        public ValidationFailure Test(LidarrSettings settings)
        {
            try
            {
                GetArtists(settings);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Error(ex, "API Key is invalid");
                    return new ValidationFailure("ApiKey", "API Key is invalid");
                }

                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("ApiKey", "Unable to send test message");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("", "Unable to send test message");
            }

            return null;
        }

        private List<TResource> Execute<TResource>(string resource, LidarrSettings settings)
        {
            if (settings.BaseUrl.IsNullOrWhiteSpace() || settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new List<TResource>();
            }

            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var request = new HttpRequestBuilder(baseUrl).Resource(resource).Accept(HttpAccept.Json)
                .SetHeader("X-Api-Key", settings.ApiKey).Build();

            var response = _httpClient.Get(request);

            var results = JsonConvert.DeserializeObject<List<TResource>>(response.Content);

            return results;
        }
    }
}
