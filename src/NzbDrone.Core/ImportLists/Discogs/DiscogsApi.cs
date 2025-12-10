using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.ImportLists.Discogs;

public class DiscogsListResponse
{
    public List<DiscogsListItem> Items { get; set; }
}

public class DiscogsListItem
{
    public string Type { get; set; }
    public int Id { get; set; }
    [JsonProperty("display_title")]
    public string DisplayTitle { get; set; }
    [JsonProperty("resource_url")]
    public string ResourceUrl { get; set; }
    public string Uri { get; set; }
}

public class DiscogsReleaseResponse
{
    public string Title { get; set; }
    public List<DiscogsReleaseArtist> Artists { get; set; }
}

public class DiscogsReleaseArtist
{
    public string Name { get; set; }
    public int Id { get; set; }
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
