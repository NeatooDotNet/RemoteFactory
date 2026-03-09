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

var sp = services.BuildServiceProvider();

// Verify class factory survived trimming (regression test).
var factory = sp.GetService<ITrimTestEntityFactory>();

// Verify static factory delegate survived trimming (the original bug scenario).
var doWorkDelegate = sp.GetService<TrimTestCommands.DoWork>();

// Direct feature switch test: verifies that the trimmer constant-folds
// NeatooRuntime.IsServerRuntime and removes dead code.
DirectFeatureSwitchTest.Run();

Console.WriteLine($"IsServerRuntime: {NeatooRuntime.IsServerRuntime}");
Console.WriteLine($"Class factory resolved: {factory != null}");
Console.WriteLine($"Static factory delegate resolved: {doWorkDelegate != null}");
Console.WriteLine("Trimming verification app completed.");
