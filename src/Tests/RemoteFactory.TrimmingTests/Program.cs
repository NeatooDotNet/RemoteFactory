using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.TrimmingTests;

// Build a service collection with RemoteFactory registrations.
// The generated FactoryServiceRegistrar will register the factory and delegates.
// Server-only service registrations are guarded by the feature switch
// so the trimmer can remove them.
var services = new ServiceCollection();

// Register the generated factory services (this calls FactoryServiceRegistrar for all [Factory] types)
// The generated code's LocalCreate is guarded by NeatooRuntime.IsServerRuntime,
// so when trimmed with IsServerRuntime=false, the server-only code path is dead code.
TrimTestEntityFactory.FactoryServiceRegistrar(services, NeatooFactory.Remote);

// Guard server-only DI registrations behind the feature switch.
// If these were registered unconditionally, the types would be kept alive
// by the DI container regardless of the feature switch in generated code.
if (NeatooRuntime.IsServerRuntime)
{
    services.AddScoped<IServerOnlyRepository, ServerOnlyRepository>();
}

var sp = services.BuildServiceProvider();

// Reference the factory interface to ensure the trimmer considers it reachable.
var factory = sp.GetService<ITrimTestEntityFactory>();

// Direct feature switch test: verifies that the trimmer constant-folds
// NeatooRuntime.IsServerRuntime and removes dead code.
DirectFeatureSwitchTest.Run();

Console.WriteLine($"IsServerRuntime: {NeatooRuntime.IsServerRuntime}");
Console.WriteLine($"Factory resolved: {factory != null}");
Console.WriteLine("Trimming verification app completed.");
