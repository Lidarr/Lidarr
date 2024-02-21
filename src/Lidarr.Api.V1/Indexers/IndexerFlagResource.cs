using Lidarr.Http.REST;
using Newtonsoft.Json;

namespace Lidarr.Api.V1.Indexers
{
    public class IndexerFlagResource : RestResource
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public new int Id { get; set; }
        public string Name { get; set; }
        public string NameLower => Name.ToLowerInvariant();
    }
}
