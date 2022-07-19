using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Plugins.Resources
{
    public class Asset
    {
        public string Url { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        [JsonProperty("content_type")]
        public string ContentType { get; set; }
        public string State { get; set; }
        public int Size { get; set; }
        [JsonProperty("download_count")]
        public int DownloadCount { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }

    public class Release
    {
        public string Url { get; set; }
        [JsonProperty("assets_url")]
        public string AssetsUrl { get; set; }
        [JsonProperty("upload_url")]
        public string UploadUrl { get; set; }
        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }
        public int Id { get; set; }
        [JsonProperty("node_id")]
        public string NodeId { get; set; }
        [JsonProperty("tag_name")]
        public string TagName { get; set; }
        [JsonProperty("target_commitish")]
        public string TargetCommitish { get; set; }
        public string Name { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }
        public List<Asset> Assets { get; set; } = new List<Asset>();
        [JsonProperty("tarball_url")]
        public string TarballUrl { get; set; }
        [JsonProperty("zipball_url")]
        public string ZipballUrl { get; set; }
        public string Body { get; set; }
    }
}
