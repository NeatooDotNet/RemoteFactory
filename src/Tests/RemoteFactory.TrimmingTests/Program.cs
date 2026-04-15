using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.TrimmingTests;

// Build a service collection with RemoteFactory registrations.
// The generated FactoryServiceRegistrar will register the factory and delegates.
// Server-only service registrations are guarded by the feature switch
// so the trimmer can remove them.
var services = new ServiceCollection();

// Exercise the assembly-attribute discovery path (RegisterFactories via AddNeatooRemoteFactory).
// This is the primary registration mechanism — it uses [assembly: NeatooFactoryRegistrar(typeof(X))]
// to discover all factory types in a trimming-safe way.
services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(TrimTestEntity).Assembly);

// Guard server-only DI registrations behind the feature switch.
// If these were registered unconditionally, the types would be kept alive
// by the DI container regardless of the feature switch in generated code.
if (NeatooRuntime.IsServerRuntime)
{
    services.AddScoped<IServerOnlyRepository, ServerOnlyRepository>();
}

ServiceProvider sp;
try
{
    sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    Console.WriteLine("ServiceProvider built successfully with ValidateOnBuild=true.");
}
catch (AggregateException ex)
{
    Console.WriteLine($"ServiceProvider construction FAILED (AggregateException):");
    foreach (var inner in ex.InnerExceptions)
    {
        Console.WriteLine($"  - {inner.GetType().Name}: {inner.Message}");
    }
    Console.WriteLine("Exiting due to service validation failure.");
    return;
}
catch (Exception ex)
{
    Console.WriteLine($"ServiceProvider construction FAILED: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine("Exiting due to service validation failure.");
    return;
}

// Verify class factory survived trimming (regression test).
ITrimTestEntityFactory? factory = null;
try
{
    factory = sp.GetService<ITrimTestEntityFactory>();
}
catch (Exception ex)
{
    Console.WriteLine($"Class factory resolution FAILED: {ex.GetType().Name}: {ex.Message}");
}

// Verify static factory delegate survived trimming (the original bug scenario).
TrimTestCommands.DoWork? doWorkDelegate = null;
try
{
    doWorkDelegate = sp.GetService<TrimTestCommands.DoWork>();
}
catch (Exception ex)
{
    Console.WriteLine($"DoWork delegate resolution FAILED: {ex.GetType().Name}: {ex.Message}");
}

// Verify event delegate resolution.
TrimTestCommands.OnWorkCompletedEvent? eventDelegate = null;
try
{
    using var scope = sp.CreateScope();
    eventDelegate = scope.ServiceProvider.GetService<TrimTestCommands.OnWorkCompletedEvent>();
}
catch (Exception ex)
{
    Console.WriteLine($"Event delegate resolution FAILED: {ex.GetType().Name}: {ex.Message}");
}

// Direct feature switch test: verifies that the trimmer constant-folds
// NeatooRuntime.IsServerRuntime and removes dead code.
DirectFeatureSwitchTest.Run();

// Event relay smoke test: verifies a FactoryEventBase descendant survives trimming
// and round-trips through FactoryEventTypeRegistry → FactoryEventDeserializer → IFactoryEventRelay.
EventRelaySmokeTest.Run();

Console.WriteLine($"IsServerRuntime: {NeatooRuntime.IsServerRuntime}");
Console.WriteLine($"Class factory resolved: {factory != null}");
Console.WriteLine($"Static factory delegate resolved: {doWorkDelegate != null}");
Console.WriteLine($"Event delegate resolved: {eventDelegate != null}");
Console.WriteLine("Trimming verification app completed.");
