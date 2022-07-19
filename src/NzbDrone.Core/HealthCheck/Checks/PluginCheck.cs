using NzbDrone.Common.Composition;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class PluginCheck : HealthCheckBase
    {
        private readonly PluginStatus _status;

        public PluginCheck(PluginStatus status, ILocalizationService localizationService)
            : base(localizationService)
        {
            _status = status;
        }

        public override HealthCheck Check()
        {
            if (!_status.Enabled)
            {
                return new HealthCheck(GetType(), HealthCheckResult.Error, "Plugins failed to load, check log for details", "#plugins-failed-to-load");
            }

            return new HealthCheck(GetType());
        }
    }
}
