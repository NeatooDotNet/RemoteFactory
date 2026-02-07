# Factory Modes

RemoteFactory has two configuration layers:

1. **Compile-time mode** (`FactoryMode`): Controls what code the generator creates
2. **Runtime mode** (`NeatooFactory`): Controls how DI and execution work

## Compile-Time Modes (Code Generation)

Set via `[assembly: FactoryMode(...)]` attribute:

| Mode | Generated Code | Use Case |
|------|----------------|----------|
| **Full** (default) | Local methods + remote handlers | Server assemblies |
| **RemoteOnly** | HTTP stubs only | Client assemblies (smaller size) |

## Runtime Modes (DI Registration)

Set via `AddNeatooRemoteFactory(NeatooFactory.XXX, ...)`:

| Mode | Execution | HTTP | Use Case |
|------|-----------|------|----------|
| **Server** | Local + handles incoming HTTP | Yes | ASP.NET Core server |
| **Remote** | HTTP calls to server | Yes | Blazor WASM, mobile |
| **Logical** | Local only | No | Console apps, tests |

## How Modes Work Together

- **Server assembly**: `FactoryMode.Full` (compile) + `NeatooFactory.Server` (runtime)
- **Client assembly**: `FactoryMode.RemoteOnly` (compile) + `NeatooFactory.Remote` (runtime)
- **Single-tier app**: `FactoryMode.Full` (compile) + `NeatooFactory.Logical` (runtime)

## Full Mode (Compile-Time)

The default code generation mode. Generates complete factories with local methods and remote delegate handlers.

### Configuration

Full mode is the default. No assembly attribute needed:

<!-- snippet: modes-full-config -->
<a id='snippet-modes-full-config'></a>
```cs
// Full mode (default): no assembly attribute needed
// services.AddNeatooRemoteFactory(NeatooFactory.Server, options, domainAssembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L8-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Generated Code

Full mode generates:
- Local methods that call entity methods directly
- Remote delegates that handle incoming HTTP requests
- Serialization converters
- Factory interface and implementation

<!-- snippet: modes-full-generated -->
<a id='snippet-modes-full-generated'></a>
```cs
// Full mode: local methods + remote handlers (dual-path execution)
// public Task<IEmployee?> Fetch(Guid id) {
//     if (MakeRemoteDelegateRequest != null) return callRemoteFetch(id);
//     return localFetchDelegate(id);  // Server/Logical: execute locally
// }
// public static void RegisterRemoteDelegates(...)  // Server: handles incoming HTTP
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/FactoryModes/GeneratedCodeIllustrations.cs#L8-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Use Full Mode For:
- Server assemblies (ASP.NET Core)
- Domain assemblies used in server projects
- Single-tier applications (console, desktop)
- Test projects that execute locally

## RemoteOnly Mode (Compile-Time)

Generates HTTP stubs only. Excludes local method implementations, producing smaller assemblies.

### Configuration

Set at assembly level with `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`:

<!-- snippet: modes-remoteonly-config -->
<a id='snippet-modes-remoteonly-config'></a>
```cs
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
// Generates HTTP stubs only - smaller client assemblies
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L25-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Place in `AssemblyAttributes.cs` or `GlobalUsings.cs`.

### Generated Code

RemoteOnly mode generates:
- HTTP call stubs that serialize and POST to `/api/neatoo`
- Serialization converters
- Factory interface (no local implementation)

<!-- snippet: modes-remoteonly-generated -->
<a id='snippet-modes-remoteonly-generated'></a>
```cs
// RemoteOnly mode: HTTP stubs only (no local implementation)
// public Task<IEmployee?> Fetch(Guid id) => callRemoteFetch(id);  // Always HTTP
// No RegisterRemoteDelegates - client doesn't handle incoming requests
// Benefits: smaller assembly, no server dependencies in client bundle
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/FactoryModes/GeneratedCodeIllustrations.cs#L17-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Benefits:
- **Smaller assemblies**: No entity method references, no server dependencies
- **Faster client startup**: Less code to load and JIT
- **Clear separation**: Client cannot accidentally call server-only code

### Use RemoteOnly Mode For:
- Blazor WebAssembly client projects
- Mobile app clients (MAUI, Xamarin)
- Desktop client projects connecting to remote servers
- Dedicated client assemblies

### Assembly Size Comparison:

**Full mode:** ~450 KB (includes entity code, EF Core types, etc.)
**RemoteOnly mode:** ~120 KB (HTTP stubs only)

Exact size depends on domain model complexity.

## Server Mode (Runtime)

Registers factories for local execution and handles incoming HTTP requests. Use with Full-generated code.

### Configuration

Server mode is typically configured via `AddNeatooAspNetCore`:

<!-- snippet: modes-server-config -->
<a id='snippet-modes-server-config'></a>
```cs
// Server mode: handles incoming HTTP + local execution
// services.AddNeatooAspNetCore(options, domainAssembly);
// app.UseNeatoo();  // Maps /api/neatoo endpoint
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L30-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-server-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Behavior

Server mode:
- Executes factory methods locally
- Registers remote delegate handlers for incoming HTTP requests
- No `IMakeRemoteDelegateRequest` (doesn't make outgoing HTTP calls)

### Use Server Mode For:
- ASP.NET Core server applications
- API projects hosting RemoteFactory endpoints

## Remote Mode (Runtime)

Registers factories that make HTTP calls to a server. Use with Full or RemoteOnly-generated code.

### Configuration

Register factories in Remote mode and configure HttpClient:

<!-- snippet: modes-remote-config -->
<a id='snippet-modes-remote-config'></a>
```cs
// Remote mode: all operations serialize and POST to server
// services.AddNeatooRemoteFactory(NeatooFactory.Remote, options, domainAssembly);
// services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
//     new HttpClient { BaseAddress = new Uri(serverUrl) });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L18-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remote-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Behavior

Remote mode:
- All operations serialize and POST to `/api/neatoo`
- Requires `HttpClient` configured with server base address
- Deserializes responses from server

### Use Remote Mode For:
- Blazor WebAssembly applications
- Mobile clients (MAUI, Xamarin)
- Desktop clients connecting to remote API
- Any client making HTTP calls to RemoteFactory server

## Logical Mode (Runtime)

Executes all methods locally without HTTP infrastructure. Use with Full-generated code.

### Configuration

<!-- snippet: modes-logical-config -->
<a id='snippet-modes-logical-config'></a>
```cs
// Logical mode: local execution, no HTTP - ideal for testing
// services.AddNeatooRemoteFactory(NeatooFactory.Logical, options, domainAssembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L13-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Behavior

Logical mode:
- Executes all factory methods locally (same as Server mode)
- No `IMakeRemoteDelegateRequest` (no outgoing HTTP)
- No remote delegate handlers (no incoming HTTP)
- Authorization still enforced

### Use Logical Mode For:
- Console applications
- Background services
- Desktop applications (WPF, WinForms)
- Unit and integration tests
- Prototyping without HTTP infrastructure

### Testing Benefits:

<!-- snippet: modes-logical-testing -->
<a id='snippet-modes-logical-testing'></a>
```cs
[Fact]
public async Task TestWithLogicalMode()
{
    // Logical mode: all operations local, no HTTP, no serialization
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddDebug());
    services.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();
    services.AddNeatooRemoteFactory(NeatooFactory.Logical,
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        typeof(Employee).Assembly);
    services.RegisterMatchingName(typeof(Employee).Assembly);
    services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
    services.AddInfrastructureServices();

    using var scope = services.BuildServiceProvider().CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IEmployeeFactory>();

    var employee = factory.Create();
    employee.FirstName = "Jane";
    employee.LastName = "Smith";
    employee.Email = new EmailAddress("jane@example.com");
    employee.DepartmentId = Guid.NewGuid();
    await factory.Save(employee);  // Executes directly, no HTTP

    var fetched = await factory.Fetch(employee.Id);
    Assert.Equal("Jane", fetched?.FirstName);
}

private class TestHostLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;
    public void StopApplication() { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/FactoryModes/LogicalModeTestingSample.cs#L18-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Tests execute locally without HTTP server or serialization overhead.

## Complete Examples

### Server Setup (Full + Server):

<!-- snippet: modes-full-example -->
<a id='snippet-modes-full-example'></a>
```cs
// Server: Full mode (default) + AddNeatooAspNetCore
// services.AddNeatooAspNetCore(options, domainAssembly);
// services.RegisterMatchingName(domainAssembly);
// app.UseNeatoo();  // /api/neatoo endpoint
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L116-L121' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Client Setup (RemoteOnly + Remote):

<!-- snippet: modes-remoteonly-example -->
<a id='snippet-modes-remoteonly-example'></a>
```cs
// Client: [assembly: FactoryMode(RemoteOnly)] + Remote runtime
// services.AddNeatooRemoteFactory(NeatooFactory.Remote, options, domainAssembly);
// services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
//     new HttpClient { BaseAddress = new Uri(serverUrl) });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L123-L128' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Single-Tier Setup (Full + Logical):

<!-- snippet: modes-logical-example -->
<a id='snippet-modes-logical-example'></a>
```cs
// Single-tier: Full mode + Logical runtime (no HTTP)
// services.AddNeatooRemoteFactory(NeatooFactory.Logical, options, domainAssembly);
// services.RegisterMatchingName(domainAssembly);
// services.AddScoped<IEmployeeRepository, EmployeeRepository>();
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L130-L135' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Typical Solution Structure

Three-tier application with server and client:

```
MySolution/
├── MySolution.Domain/           # Full (compile) + used by server
├── MySolution.Server/           # Full (compile) + Server (runtime)
└── MySolution.Client/           # RemoteOnly (compile) + Remote (runtime)
```

**Domain project:** `FactoryMode.Full` (default, no attribute)
**Server project:** `FactoryMode.Full` + `AddNeatooAspNetCore()` (uses Server mode)
**Client project:** `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` + `AddNeatooRemoteFactory(NeatooFactory.Remote, ...)`

## Switching Modes

### Changing Compile-Time Mode

Add or change `[assembly: FactoryMode(...)]`:

**Before (Full):**
```csharp
// No attribute = Full mode (default)
```

**After (RemoteOnly):**
```csharp
[assembly: FactoryMode(FactoryMode.RemoteOnly)]
```

Rebuild to regenerate factories.

### Changing Runtime Mode

Change the `NeatooFactory` argument:

```csharp
// Before
services.AddNeatooRemoteFactory(NeatooFactory.Server, ...);

// After
services.AddNeatooRemoteFactory(NeatooFactory.Logical, ...);
```

## Local vs Remote Methods

The `[Remote]` attribute controls whether entity methods can be called over HTTP:

<!-- snippet: modes-local-remote-methods -->
<a id='snippet-modes-local-remote-methods'></a>
```cs
// No [Remote]: runs locally on client, no serialization
[Fetch]
public void FetchLocal(string data) => FirstName = data;

// [Remote]: serializes and executes on server
[Remote, Fetch]
public async Task<bool> FetchFromServer(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
{
    var entity = await repo.GetByIdAsync(id, ct);
    if (entity == null) return false;
    Id = entity.Id;
    FirstName = entity.FirstName;
    return true;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L19-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-local-remote-methods' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**How methods execute by mode:**

- **FactoryMode.Full + NeatooFactory.Server/Logical**: Both methods execute locally
- **FactoryMode.Full + NeatooFactory.Remote**: Both methods make HTTP calls
- **FactoryMode.RemoteOnly + NeatooFactory.Remote**: Both methods make HTTP calls (no local implementation available)

## Generated Code Differences

### Full Mode Factory:

Generated factories have local methods and can handle remote requests:

```csharp
class PersonFactory : IPersonFactory
{
    private readonly IServiceProvider ServiceProvider;
    private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

    public IPerson Create() => new Person();
    public Task<IPerson> Fetch(int id) => localDelegate(id);

    // Static methods for handling incoming HTTP (Server mode)
    public static void RegisterRemoteDelegates(HandleRemoteDelegateRequest handler) { ... }
}
```

### RemoteOnly Mode Factory:

Generated factories only have HTTP call stubs:

```csharp
class PersonFactory : IPersonFactory
{
    private readonly IServiceProvider ServiceProvider;
    private readonly IMakeRemoteDelegateRequest MakeRemoteDelegateRequest;

    public IPerson Create() => callRemoteCreate();
    public Task<IPerson> Fetch(int id) => callRemoteFetch(id);

    // No local implementation
    // No static remote delegate handlers
}
```

## Debugging

### By Compile-Time Mode:

- **Full mode**: Set breakpoints in entity methods and factory code
- **RemoteOnly mode**: Set breakpoints in HTTP call logic, inspect serialized payloads

### By Runtime Mode:

- **Server mode**: Set breakpoints in entity methods (local execution) and remote delegate handlers
- **Remote mode**: Set breakpoints in HTTP client code, inspect requests/responses
- **Logical mode**: Set breakpoints in entity methods (local execution)

### Enable verbose logging:

<!-- snippet: modes-logging -->
<a id='snippet-modes-logging'></a>
```cs
// Enable verbose logging to trace factory execution
// services.AddLogging(b => b.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace));
// Logs: "Executing local factory method..." or "Sending remote factory request..."
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/FactoryModeConfigurationSamples.cs#L19-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logging' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Logs show mode, serialization events, and HTTP calls.

## Performance Characteristics

### Compile-Time Modes:

| Mode | Assembly Size | Load Time | Code Generated |
|------|---------------|-----------|----------------|
| **Full** | Larger (~450 KB) | Slower | Local + Remote handlers |
| **RemoteOnly** | Smaller (~120 KB) | Faster | HTTP stubs only |

### Runtime Modes:

| Mode | Call Overhead | Serialization | HTTP |
|------|---------------|---------------|------|
| **Server** | None (local) | Only for incoming requests | Yes (incoming) |
| **Remote** | HTTP + JSON | Yes (both ways) | Yes (outgoing) |
| **Logical** | None | None | No |

## Next Steps

- [Getting Started](getting-started.md) - Configure modes in a solution
- [Serialization](serialization.md) - Ordinal vs Named formats
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server mode setup
