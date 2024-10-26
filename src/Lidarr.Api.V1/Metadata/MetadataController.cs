using System;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Metadata
{
    [V1ApiController]
    public class MetadataController : ProviderControllerBase<MetadataResource, MetadataBulkResource, IMetadata, MetadataDefinition>
    {
        public static readonly MetadataResourceMapper ResourceMapper = new ();
        public static readonly MetadataBulkResourceMapper BulkResourceMapper = new ();

        public MetadataController(IBroadcastSignalRMessage signalRBroadcaster, IMetadataFactory metadataFactory)
            : base(signalRBroadcaster, metadataFactory, "metadata", ResourceMapper, BulkResourceMapper)
        {
        }

        [NonAction]
        public override ActionResult<MetadataResource> UpdateProvider([FromBody] MetadataBulkResource providerResource)
        {
            throw new NotImplementedException();
        }

        [NonAction]
        public override object DeleteProviders([FromBody] MetadataBulkResource resource)
        {
            throw new NotImplementedException();
        }
    }
}
