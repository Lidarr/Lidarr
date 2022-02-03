using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Lidarr.Http;

public class VersionedFeedControllerAttribute : Attribute, IRouteTemplateProvider
{
    public VersionedFeedControllerAttribute(int version, string resource = "[controller]")
    {
        Version = version;
        Template = $"feed/v{Version}/{resource}";
    }

    public string Template { get; private set; }
    public int? Order => 2;
    public string Name { get; set; }
    public int Version { get; private set; }
}

public class V1FeedControllerAttribute : VersionedFeedControllerAttribute
{
    public V1FeedControllerAttribute(string resource = "[controller]")
        : base(1, resource)
    {
    }
}
