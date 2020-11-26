using System;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download.Clients.Deemix;

namespace NzbDrone.Core.Indexers.Deemix
{
    public class DeemixRequest : IndexerRequest
    {
        public DeemixRequest(string url, Func<DeemixProxy, DeemixSearchResponse> request)
        : base(url, HttpAccept.Json)
        {
            Request = request;
        }

        public Func<DeemixProxy, DeemixSearchResponse> Request { get; set; }
    }
}
