/// <summary>
/// Code samples for docs/reference/factory-modes.md
///
/// Snippets in this file:
/// - docs:reference/factory-modes:server-configuration
/// </summary>

using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using RemoteFactory.Samples.DomainModel.FactoryOperations;

namespace RemoteFactory.Samples.Server;

/// <summary>
/// Server mode configuration examples.
/// </summary>
public static class ServerConfigSamples
{
    public static void ConfigureServerMode(IServiceCollection services)
    {
#region docs:reference/factory-modes:server-configuration
// Using the AspNetCore helper (recommended)
services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);

// Or using the base method directly
services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(IPersonModel).Assembly);
#endregion
    }
}
