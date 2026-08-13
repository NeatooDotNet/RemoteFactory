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

// Remote-mode factories resolve a keyed HttpClient for the server call channel
// (standard client setup, e.g. Design.Client.Blazor). The harness never sends a
// request — the registration only has to satisfy resolution. A no-op handler
// keeps SocketsHttpHandler (and its System.Net.Security dependency, trimmed out
// of this full-trim publish) from being constructed.
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient(new NoOpHttpHandler()) { BaseAddress = new Uri("https://localhost/") });

// Guard server-only DI registrations behind the feature switch.
// If these were registered unconditionally, the types would be kept alive
// by the DI container regardless of the feature switch in generated code.
if (NeatooRuntime.IsServerRuntime)
{
    services.AddScoped<IServerOnlyRepository, ServerOnlyRepository>();

    // Per-leg server-only dependencies (TRIM-008). Same guard, same reason: a
    // registration outside it would root the implementation from the DI graph
    // and mask whatever the generator's own preservation is doing.
    services.AddScoped<IRelayLegPort, RelayLegBackend>();
    services.AddScoped<IIfaceLegPort, IfaceLegBackend>();
    services.AddScoped<ISaveLegPort, SaveLegBackend>();

    // Server-side implementation behind the interface factory. On the client the
    // generated proxy stands in for it, so it is never registered there.
    services.AddScoped<ITrimIfaceQuery, TrimIfaceServerSide>();
}

// Every named check appends to failedChecks; the process exits non-zero if any
// check failed. Per-check console lines stay as the failure forensics in CI logs.
var failedChecks = new List<string>();

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
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"ServiceProvider construction FAILED: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine("Exiting due to service validation failure.");
    return 1;
}

// Factories are registered scoped; resolve within a scope (root resolution
// throws under ValidateScopes=true).
using var checkScope = sp.CreateScope();

// Verify class factory survived trimming (regression test).
ITrimTestEntityFactory? factory = null;
try
{
    factory = checkScope.ServiceProvider.GetService<ITrimTestEntityFactory>();
}
catch (Exception ex)
{
    Console.WriteLine($"Class factory resolution FAILED: {ex.GetType().Name}: {ex.Message}");
}
if (factory == null)
{
    failedChecks.Add("class factory resolution");
}

// Verify static factory delegate survived trimming (the original bug scenario).
TrimTestCommands.DoWork? doWorkDelegate = null;
try
{
    doWorkDelegate = checkScope.ServiceProvider.GetService<TrimTestCommands.DoWork>();
}
catch (Exception ex)
{
    Console.WriteLine($"DoWork delegate resolution FAILED: {ex.GetType().Name}: {ex.Message}");
}
if (doWorkDelegate == null)
{
    failedChecks.Add("static factory delegate resolution");
}

// Verify the interface-factory leg still registers after trimming (TRIM-008).
// This is the positive control for that leg: the DAM-preservation question is
// asked with absence greps, and an absence grep passes just as well when the
// feature is dead. Resolving the generated proxy proves it is not.
ITrimIfaceQueryFactory? ifaceFactory = null;
try
{
    ifaceFactory = checkScope.ServiceProvider.GetService<ITrimIfaceQueryFactory>();
}
catch (Exception ex)
{
    Console.WriteLine($"Interface factory resolution FAILED: {ex.GetType().Name}: {ex.Message}");
}
if (ifaceFactory == null)
{
    failedChecks.Add("interface factory resolution");
}

// Verify the Save/Can* leg still registers after trimming (TRIM-008).
// Positive control for the write half of the class-factory shape.
ITrimSaveTargetFactory? saveFactory = null;
try
{
    saveFactory = checkScope.ServiceProvider.GetService<ITrimSaveTargetFactory>();
}
catch (Exception ex)
{
    Console.WriteLine($"Save target factory resolution FAILED: {ex.GetType().Name}: {ex.Message}");
}
if (saveFactory == null)
{
    failedChecks.Add("save target factory resolution");
}

// NOTE: the [FactoryEventHandler<T>] leg has NO positive control here, and cannot.
// RelayHandlerRenderer wraps every RegisterHandler call in
// `if (NeatooRuntime.IsServerRuntime)`, so on a client publish the generated
// registrar body folds away entirely — there is no service, delegate, or registry
// entry left to resolve. Its registration counter-signal lives in the untrimmed
// integration suite (FactoryEventHandlerTargets), not in this harness.

// Direct feature switch test: verifies that the trimmer constant-folds
// NeatooRuntime.IsServerRuntime and removes dead code.
if (!DirectFeatureSwitchTest.Run())
{
    failedChecks.Add("feature switch constant fold");
}

// Event relay smoke test: verifies a FactoryEventBase descendant survives trimming
// and round-trips through FactoryEventTypeRegistry → FactoryEventDeserializer → IFactoryEventRelay.
if (!EventRelaySmokeTest.Run())
{
    failedChecks.Add("event relay smoke");
}

// Record DTO smoke test (TRIM-001): positional records in factory signatures are
// preserved by generator-emitted PreserveType and deserialize on the trimmed client.
if (!RecordDtoSmokeTest.Run())
{
    failedChecks.Add("record DTO preservation");
}

// Entity property DTO smoke test (TRIM-002): DTOs reachable only as [Factory]
// entity properties are preserved by the entity property-graph walk.
if (!EntityPropertyDtoSmokeTest.Run())
{
    failedChecks.Add("entity property DTO preservation");
}

// Subscribe-only event smoke test (TRIM-003 repro / TRIM-007 fix): a
// FactoryEventBase descendant whose only static reference is a generic
// Subscribe<TEvent> call site survives trimming via the generator-emitted
// per-assembly event-preservation registrar.
if (!EventSubscribeOnlySmokeTest.Run())
{
    failedChecks.Add("subscribe-only event preservation");
}

Console.WriteLine($"IsServerRuntime: {NeatooRuntime.IsServerRuntime}");
Console.WriteLine($"Class factory resolved: {factory != null}");
Console.WriteLine($"Static factory delegate resolved: {doWorkDelegate != null}");
Console.WriteLine($"Interface factory resolved: {ifaceFactory != null}");
Console.WriteLine($"Save target factory resolved: {saveFactory != null}");

if (failedChecks.Count > 0)
{
    Console.WriteLine($"Trimming verification FAILED ({failedChecks.Count} check(s)): {string.Join(", ", failedChecks)}");
    return 1;
}

Console.WriteLine("Trimming verification app completed. All checks passed.");
return 0;

/// <summary>
/// The harness resolves Remote-mode factories but never calls the server;
/// any actual send is a harness bug.
/// </summary>
internal sealed class NoOpHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw new NotSupportedException("The trimming harness never sends HTTP requests.");
}
