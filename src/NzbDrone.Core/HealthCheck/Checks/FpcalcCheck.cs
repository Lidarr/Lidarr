using NzbDrone.Core.Parser;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class FpcalcCheck : HealthCheckBase
    {
        private readonly IFingerprintingService _fingerprintingService;
        
        public FpcalcCheck(IFingerprintingService fingerprintingService)
        {
            _fingerprintingService = fingerprintingService;
        }
        
        public override HealthCheck Check()
        {
            if (!_fingerprintingService.IsSetup())
            {
                return new HealthCheck(GetType(), HealthCheckResult.Warning, $"fpcalc could not be found.  Audio fingerprinting disabled.", "#fpcalc-missing");
            }

            return new HealthCheck(GetType());
        }
    }
}
