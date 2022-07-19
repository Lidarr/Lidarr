using NzbDrone.Core.Profiles.Delay;

namespace Lidarr.Api.V1.Profiles.Delay
{
    public class DelayProfileProtocolItemResource
    {
        public string Name { get; set; }
        public string Protocol { get; set; }
        public bool Allowed { get; set; }
        public int Delay { get; set; }
    }

    public static class ProfileItemResourceMapper
    {
        public static DelayProfileProtocolItemResource ToResource(this DelayProfileProtocolItem model)
        {
            if (model == null)
            {
                return null;
            }

            return new DelayProfileProtocolItemResource
            {
                Name = model.Name,
                Protocol = model.Protocol,
                Allowed = model.Allowed,
                Delay = model.Delay
            };
        }

        public static DelayProfileProtocolItem ToModel(this DelayProfileProtocolItemResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new DelayProfileProtocolItem
            {
                Name = resource.Name,
                Protocol = resource.Protocol,
                Allowed = resource.Allowed,
                Delay = resource.Delay
            };
        }
    }
}
