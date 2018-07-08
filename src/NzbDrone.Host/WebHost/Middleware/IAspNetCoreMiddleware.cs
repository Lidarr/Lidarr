using Microsoft.AspNetCore.Builder;

namespace NzbDrone.Host.WebHost.Middleware
{
    public interface IAspNetCoreMiddleware
    {
        int Order { get; }
        void Attach(IApplicationBuilder appBuilder);
    }
}
