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
// Full mode (compile-time) is the default - no attribute needed
// Or explicitly: [assembly: FactoryMode(FactoryMode.Full)]

public static class FullModeConfiguration
{
    public static void ConfigureServer(IServiceCollection services)
    {
        // Full mode assembly + Server runtime mode
        // Full mode generates both local execution and remote handler code
        services.AddNeatooRemoteFactory(
            NeatooFactory.Server, // Runtime mode: handles incoming HTTP
            typeof(FactoryModesSamples).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L13-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-config' title='Start of snippet'>anchor</a></sup>
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
// In Full mode, the generated factory has:
// 1. Local execution path - calls entity methods directly
// 2. Remote execution path - serializes and sends via HTTP (when IMakeRemoteDelegateRequest is registered)
//
// Generated factory (simplified):
// public partial class EntityFactory : IEntityFactory
// {
//     private readonly IMakeRemoteDelegateRequest? _remoteRequest;
//
//     public async Task<Entity> Create()
//     {
//         if (_remoteRequest != null)
//         {
//             // Remote execution - serialize and send
//             return await _remoteRequest.ForDelegate<Entity>(...);
//         }
//         else
//         {
//             // Local execution - call directly
//             return new Entity();
//         }
//     }
// }
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L30-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-generated' title='Start of snippet'>anchor</a></sup>
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
// In client assembly's AssemblyAttributes.cs:
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]

public static class RemoteOnlyConfiguration
{
    public static void ConfigureClient(IServiceCollection services, string serverUrl)
    {
        // RemoteOnly mode assembly + Remote runtime mode
        // RemoteOnly generates HTTP stubs only (smaller assembly)
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote, // Runtime mode: makes HTTP calls to server
            typeof(FactoryModesSamples).Assembly);

        // Must register HttpClient for remote calls
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient { BaseAddress = new Uri(serverUrl) });
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L56-L76' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-config' title='Start of snippet'>anchor</a></sup>
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
// In RemoteOnly mode, the generated factory only has HTTP stubs:
//
// Generated factory (simplified):
// public partial class EntityFactory : IEntityFactory
// {
//     private readonly IMakeRemoteDelegateRequest _remoteRequest;
//
//     public async Task<Entity> Create()
//     {
//         // Always remote - serialize and send via HTTP
//         return await _remoteRequest.ForDelegate<Entity>(...);
//     }
// }
//
// Benefits:
// - Smaller assembly size (no local implementation code)
// - No server-side dependencies (DbContext, repositories, etc.)
// - Clear client/server separation
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L78-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-generated' title='Start of snippet'>anchor</a></sup>
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
public static class ServerModeConfiguration
{
    public static void ConfigureServer(IServiceCollection services)
    {
        // Server mode - handles incoming HTTP requests from clients
        // AddNeatooAspNetCore uses NeatooFactory.Server internally
        services.AddNeatooAspNetCore(typeof(FactoryModesSamples).Assembly);

        // Register server-side services (repositories, DbContext, etc.)
        services.AddScoped<IPersonRepository, PersonRepository>();
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L99-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-server-config' title='Start of snippet'>anchor</a></sup>
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
public static class RemoteModeConfiguration
{
    public static void ConfigureClient(IServiceCollection services, string serverUrl)
    {
        // Remote mode - all factory operations go via HTTP to server
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(FactoryModesSamples).Assembly);

        // Register HttpClient with the server's base address
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient { BaseAddress = new Uri(serverUrl) });
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L114-L130' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remote-config' title='Start of snippet'>anchor</a></sup>
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
public static class LogicalModeConfiguration
{
    public static void ConfigureLogical(IServiceCollection services)
    {
        // Logical mode - direct execution, no serialization
        // Use for single-tier apps or unit tests
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            typeof(FactoryModesSamples).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L132-L144' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-config' title='Start of snippet'>anchor</a></sup>
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
public partial class LogicalModeTestingExample
{
    // [Fact]
    public async Task TestDomainLogic_WithoutHttp()
    {
        // Create container in Logical mode
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostApplicationLifetime, TestHostApplicationLifetime>();

        // Logical mode - direct execution
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            typeof(FactoryModesSamples).Assembly);

        // Register domain services
        services.AddSingleton<IPersonRepository, PersonRepository>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeEntityFactory>();

        // Test domain logic without HTTP overhead
        var entity = factory.Create();
        entity.Name = "Test";

        // Method executes directly, no serialization
        await factory.Fetch(Guid.NewGuid());
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L146-L178' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Tests execute locally without HTTP server or serialization overhead.

## Complete Examples

### Server Setup (Full + Server):

<!-- snippet: modes-full-example -->
<a id='snippet-modes-full-example'></a>
```cs
// Complete server setup with Full mode

[Factory]
public partial class ServerModeEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ServerModeEntity() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IPersonRepository repository)
    {
        await repository.AddAsync(new PersonEntity
        {
            Id = Id,
            FirstName = Name,
            LastName = string.Empty,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        });
        await repository.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id);
        if (entity != null)
        {
            entity.FirstName = Name;
            entity.Modified = DateTime.UtcNow;
            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();
        }
    }
}

public static class FullModeServerSetup
{
    public static void Configure(IServiceCollection services)
    {
        // Server mode - handles incoming HTTP requests
        services.AddNeatooAspNetCore(typeof(FactoryModesSamples).Assembly);

        // Register server-only services
        services.AddScoped<IPersonRepository, PersonRepository>();
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L197-L263' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Client Setup (RemoteOnly + Remote):

<!-- snippet: modes-remoteonly-example -->
<a id='snippet-modes-remoteonly-example'></a>
```cs
// Complete client setup with RemoteOnly mode

public static class RemoteOnlyClientSetup
{
    public static void Configure(IServiceCollection services, string serverUrl)
    {
        // Remote mode - all operations go via HTTP
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(FactoryModesSamples).Assembly);

        // Register keyed HttpClient
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient
            {
                BaseAddress = new Uri(serverUrl),
                Timeout = TimeSpan.FromSeconds(30)
            });

        // Client-side services only
        services.AddSingleton<IClientLoggerService, ClientLoggerService>();
    }
}

public interface IClientLoggerService
{
    void Log(string message);
}

public partial class ClientLoggerService : IClientLoggerService
{
    public void Log(string message) => Console.WriteLine(message);
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L265-L300' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Single-Tier Setup (Full + Logical):

<!-- snippet: modes-logical-example -->
<a id='snippet-modes-logical-example'></a>
```cs
// Complete single-tier setup with Logical mode

public static class LogicalModeSetup
{
    public static void Configure(IServiceCollection services)
    {
        // Logical mode - direct execution, no HTTP
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            typeof(FactoryModesSamples).Assembly);

        // All services available locally
        services.AddSingleton<IPersonRepository, PersonRepository>();
        services.AddSingleton<IOrderRepository, OrderRepository>();
    }
}

public partial class SingleTierAppExample
{
    // [Fact]
    public async Task RunLocally()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostApplicationLifetime, TestHostApplicationLifetime>();
        LogicalModeSetup.Configure(services);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeEntityFactory>();

        var entity = factory.Create();
        entity.Name = "Local Entity";

        // Executes directly - no HTTP, no serialization
        Assert.NotNull(entity);
        Assert.Equal("Local Entity", entity.Name);
        await Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L302-L344' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-example' title='Start of snippet'>anchor</a></sup>
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
[Factory]
public partial class MixedModeEntity
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public string LocalOnlyData { get; private set; } = string.Empty;
    public string RemoteData { get; private set; } = string.Empty;

    [Create]
    public MixedModeEntity() { Id = Guid.NewGuid(); }

    // Local-only method - executes on client/server directly
    // No [Remote] attribute means this only runs locally
    [Fetch]
    public void FetchLocal(string data)
    {
        LocalOnlyData = data;
    }

    // Remote method - serializes and executes on server
    [Remote]
    [Fetch]
    public async Task FetchRemote(Guid id, [Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity != null)
        {
            Id = entity.Id;
            Name = entity.FirstName;
            RemoteData = "Loaded from server";
        }
    }
}
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L346-L380' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-local-remote-methods' title='Start of snippet'>anchor</a></sup>
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
public static class ModeLogging
{
    public static void ConfigureWithLogging(IServiceCollection services, NeatooFactory mode)
    {
        // Configure logging to see mode behavior
        // services.AddLogging(builder =>
        // {
        //     builder.AddConsole();
        //     builder.SetMinimumLevel(LogLevel.Debug);
        //     builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
        // });

        services.AddNeatooRemoteFactory(mode, typeof(FactoryModesSamples).Assembly);
    }
}

// Logs will show:
// - "Executing local factory method..." for Server/Logical modes
// - "Sending remote factory request..." for Remote mode
// - Serialization format and payload size
```
<sup><a href='/src/docs/samples/FactoryModesSamples.cs#L382-L403' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logging' title='Start of snippet'>anchor</a></sup>
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
