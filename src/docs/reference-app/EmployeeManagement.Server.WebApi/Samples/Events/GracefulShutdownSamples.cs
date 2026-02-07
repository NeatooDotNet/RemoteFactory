using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.Events;

// Region removed - events-graceful-shutdown now in EventsSamples.cs (minimal version)
// Full demo kept for reference
public static class EventGracefulShutdownConfig
{
    public static void ConfigureEventTracking(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;
        // AddNeatooAspNetCore registers IEventTracker + EventTrackerHostedService
        services.AddNeatooAspNetCore(domainAssembly);
    }
}
