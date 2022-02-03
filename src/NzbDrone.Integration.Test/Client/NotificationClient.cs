using System.Collections.Generic;
using Lidarr.Api.V1.Notifications;
using RestSharp;

namespace NzbDrone.Integration.Test.Client;

public class NotificationClient : ClientBase<NotificationResource>
{
    public NotificationClient(IRestClient restClient, string apiKey)
        : base(restClient, apiKey)
    {
    }

    public List<NotificationResource> Schema()
    {
        var request = BuildRequest("/schema");
        return Get<List<NotificationResource>>(request);
    }
}
