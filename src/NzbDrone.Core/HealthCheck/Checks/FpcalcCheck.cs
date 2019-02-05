using System;
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

            var fpcalcVersion = _fingerprintingService.FpcalcVersion();
            if (fpcalcVersion == null || fpcalcVersion < new Version("1.4.3"))
            {
                return new HealthCheck(GetType(), HealthCheckResult.Warning, $"You have an old version of fpcalc.  Please upgrade to 1.4.3.", "#fpcalc-upgrade");
            }

            return new HealthCheck(GetType());
        }
    }
}
