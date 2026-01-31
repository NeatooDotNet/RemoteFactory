using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.Events;

#region events-graceful-shutdown
/// <summary>
/// Demonstrates ASP.NET Core event tracking configuration.
/// </summary>
public static class EventGracefulShutdownConfig
{
    public static void ConfigureEventTracking(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // AddNeatooAspNetCore registers:
        // - IEventTracker (singleton): Monitors pending fire-and-forget events
        // - EventTrackerHostedService (IHostedService): Handles graceful shutdown
        services.AddNeatooAspNetCore(domainAssembly);

        // Shutdown sequence:
        // 1. ApplicationStopping token is triggered
        // 2. EventTrackerHostedService.StopAsync is called
        // 3. EventTrackerHostedService waits for pending events via WaitAllAsync
        // 4. Running events receive cancellation signal
        // 5. Application exits after events complete (or timeout)
    }
}
#endregion
