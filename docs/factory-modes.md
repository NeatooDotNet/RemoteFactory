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
// Full mode configuration in Program.cs
// Generates both local implementation and remote HTTP stubs
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Full,  // Full mode
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L17-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-full-config-1'></a>
```cs
// Full mode: Both local methods and remote stubs generated
// Use in shared domain assemblies
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Full,  // Generate both local and remote code
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L165-L172' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-config-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Generated Code

Full mode generates:
- Local methods that call entity methods directly
- Remote delegates that handle incoming HTTP requests
- Serialization converters
- Factory interface and implementation

Full mode generates factories with both local execution methods and remote HTTP stubs:

```csharp
// Source: Generated factory from Generated/Neatoo.Generator/...
// Full mode factory - contains both Local and Remote methods
internal class TestAggregateFactory : FactorySaveBase<ITestAggregate>, ITestAggregateFactory
{
    private readonly IServiceProvider ServiceProvider;
    private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

    // Delegates for local/remote routing
    public delegate Task<ITestAggregate> FetchDelegate(Guid id, CancellationToken ct = default);
    public FetchDelegate FetchProperty { get; }

    // Constructor for local execution (Server/Logical mode)
    public TestAggregateFactory(IServiceProvider sp, IFactoryCore<ITestAggregate> core) : base(core)
    {
        ServiceProvider = sp;
        FetchProperty = LocalFetch;  // Routes to local method
    }

    // Constructor for remote execution (Remote mode)
    public TestAggregateFactory(IServiceProvider sp, IMakeRemoteDelegateRequest remote,
        IFactoryCore<ITestAggregate> core) : base(core)
    {
        ServiceProvider = sp;
        MakeRemoteDelegateRequest = remote;
        FetchProperty = RemoteFetch;  // Routes to HTTP call
    }

    // Public method - delegates to local or remote based on constructor
    public virtual Task<ITestAggregate> Fetch(Guid id, CancellationToken ct = default)
        => FetchProperty(id, ct);

    // LOCAL execution - resolves services, calls domain method
    public Task<ITestAggregate> LocalFetch(Guid id, CancellationToken ct = default)
    {
        var target = ServiceProvider.GetRequiredService<TestAggregate>();
        var dataStore = ServiceProvider.GetRequiredService<ITestDataStore>();
        return DoFactoryMethodCallAsync(target, FactoryOperation.Fetch,
            () => target.Fetch(id, dataStore));
    }

    // REMOTE execution - serializes call to HTTP
    public virtual async Task<ITestAggregate> RemoteFetch(Guid id, CancellationToken ct = default)
        => (await MakeRemoteDelegateRequest!.ForDelegate<ITestAggregate>(
            typeof(FetchDelegate), [id], ct))!;
}
```

*Source: Pattern from `Generated/Neatoo.Generator/Neatoo.Factory/` - actual generated factories follow this structure*

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
// RemoteOnly mode assembly attribute
// Client assemblies that only generate HTTP stubs
// Use when you have separate client and server projects
[assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L48-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Place in `AssemblyAttributes.cs` or `GlobalUsings.cs`.

### Generated Code

RemoteOnly mode generates:
- HTTP call stubs that serialize and POST to `/api/neatoo`
- Serialization converters
- Factory interface (no local implementation)

RemoteOnly mode generates factories with HTTP stubs only - no local implementation:

```csharp
// Source: Generated factory from Generated/Neatoo.Generator/...
// RemoteOnly mode factory - HTTP stubs only, no Local methods
internal class TestAggregateFactory : FactorySaveBase<ITestAggregate>, ITestAggregateFactory
{
    private readonly IServiceProvider ServiceProvider;
    private readonly IMakeRemoteDelegateRequest MakeRemoteDelegateRequest;  // Required, not optional

    // Delegates (same signature as Full mode)
    public delegate Task<ITestAggregate> FetchDelegate(Guid id, CancellationToken ct = default);
    public FetchDelegate FetchProperty { get; }

    // Single constructor - always routes to remote
    public TestAggregateFactory(IServiceProvider sp, IMakeRemoteDelegateRequest remote,
        IFactoryCore<ITestAggregate> core) : base(core)
    {
        ServiceProvider = sp;
        MakeRemoteDelegateRequest = remote;
        FetchProperty = RemoteFetch;  // Always remote
    }

    // Public method delegates to remote only
    public virtual Task<ITestAggregate> Fetch(Guid id, CancellationToken ct = default)
        => FetchProperty(id, ct);

    // REMOTE execution only - no LocalFetch method exists
    public virtual async Task<ITestAggregate> RemoteFetch(Guid id, CancellationToken ct = default)
        => (await MakeRemoteDelegateRequest!.ForDelegate<ITestAggregate>(
            typeof(FetchDelegate), [id], ct))!;

    // Note: No LocalFetch, LocalInsert, LocalUpdate, LocalDelete methods
    // Note: No service resolution for domain services (ITestDataStore, etc.)
    // Result: Smaller assembly, faster client startup
}
```

*Source: Pattern from `Generated/Neatoo.Generator/Neatoo.Factory/` in RemoteOnly projects*

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
// Server mode configuration with ASP.NET Core integration
// Handles incoming remote requests from clients
builder.Services.AddNeatooAspNetCore(
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

var app = builder.Build();
app.UseNeatoo();  // Maps /api/neatoo endpoint
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L55-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-server-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-server-config-1'></a>
```cs
// Server mode: Handle remote requests via ASP.NET Core
// Use in server applications
builder.Services.AddNeatooAspNetCore(
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L196-L202' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-server-config-1' title='Start of snippet'>anchor</a></sup>
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
// Remote mode configuration for Blazor WASM clients
// Generates HTTP stubs that call server
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,  // Remote mode - HTTP client stubs
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Required: Register HttpClient for remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri("https://api.example.com/") });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L35-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remote-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-remote-config-1'></a>
```cs
// Remote mode: Client-side HTTP stubs only
// Use in Blazor WASM clients
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,  // Generate HTTP stubs for remote calls
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Required: Register HttpClient for remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri("https://api.example.com/") });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L183-L194' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remote-config-1' title='Start of snippet'>anchor</a></sup>
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
// Logical mode configuration for testing
// All methods execute locally without serialization
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Logical,  // Logical mode - no HTTP, no serialization
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L26-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-logical-config-1'></a>
```cs
// Logical mode: Everything runs locally, no HTTP
// Use for testing
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Logical,  // All methods local, no serialization
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L174-L181' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-config-1' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Employee for Logical mode testing - all operations run locally.
/// </summary>
[Factory]
public partial class EmployeeLogicalMode : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    // Logical mode: All methods execute locally in the same process
    // [Remote] attribute is honored but no serialization occurs
    // Ideal for unit testing domain logic without mocking HTTP

    [Create]
    public EmployeeLogicalMode()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// In Logical mode, runs locally with local DI resolution.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L73-L157' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Tests execute locally without HTTP server or serialization overhead.

## Complete Examples

### Server Setup (Full + Server):

<!-- snippet: modes-full-example -->
<a id='snippet-modes-full-example'></a>
```cs
// Complete server setup: Full mode (compile) + Server runtime.
// Use this configuration for ASP.NET Core server applications.
//
// In Program.cs:
//
// var domainAssembly = typeof(Employee).Assembly;
//
// // Register RemoteFactory with ASP.NET Core integration
// builder.Services.AddNeatooAspNetCore(
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     domainAssembly);
//
// // Register interface -> implementation mappings
// builder.Services.RegisterMatchingName(domainAssembly);
//
// // Configure middleware
// app.UseAuthentication();
// app.UseAuthorization();
// app.UseNeatoo(); // Add /api/neatoo endpoint
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L206-L226' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-example' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-full-example-1'></a>
```cs
/// <summary>
/// Full mode example - generates both local and remote code.
/// </summary>
public class FullModeExample
{
    [Fact]
    public async Task FullMode_LocalAndRemoteCode()
    {
        // Full mode generates:
        // - Local method implementations
        // - Remote HTTP stubs
        // Use in shared domain assemblies

        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Local operations (no network)
        var employee = factory.Create();
        employee.FirstName = "FullMode";
        employee.Email = new EmailAddress("full.mode@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Remote operations (would use HTTP in Remote mode)
        await factory.Save(employee);
        var fetched = await factory.Fetch(employee.Id);

        Assert.NotNull(fetched);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L719-L751' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-example-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Client Setup (RemoteOnly + Remote):

<!-- snippet: modes-remoteonly-example -->
<a id='snippet-modes-remoteonly-example'></a>
```cs
// Complete client setup: RemoteOnly mode (compile) + Remote runtime.
// Use this configuration for Blazor WASM or other HTTP clients.
//
// In AssemblyAttributes.cs:
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
//
// In Program.cs:
// var domainAssembly = typeof(Employee).Assembly;
//
// // Register RemoteFactory in Remote mode
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Remote,
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     domainAssembly);
//
// // Required: Register keyed HttpClient for remote calls
// services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
//     new HttpClient { BaseAddress = new Uri("https://api.example.com/") });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L228-L247' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-example' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-remoteonly-example-1'></a>
```cs
/// <summary>
/// RemoteOnly mode example - HTTP stubs for client assemblies.
/// </summary>
public class RemoteOnlyModeExample
{
    [Fact]
    public void RemoteOnlyMode_GeneratesHttpStubs()
    {
        // RemoteOnly mode:
        // - Generates HTTP client stubs only
        // - No local method implementations
        // - Use in Blazor WASM client assemblies

        // Configuration:
        // [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
        // services.AddNeatooRemoteFactory(NeatooFactory.Remote, options, assembly);
        // services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, ...);

        // All [Remote] methods become HTTP calls to /api/neatoo
        Assert.True(true);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L787-L810' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-example-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Single-Tier Setup (Full + Logical):

<!-- snippet: modes-logical-example -->
<a id='snippet-modes-logical-example'></a>
```cs
// Complete single-tier setup: Full mode (compile) + Logical runtime.
// Use this configuration for console apps, tests, or single-tier apps.
//
// In Program.cs:
// var domainAssembly = typeof(Employee).Assembly;
//
// // Register RemoteFactory in Logical mode
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Logical,
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     domainAssembly);
//
// // Register interface -> implementation mappings
// services.RegisterMatchingName(domainAssembly);
//
// // Register infrastructure services directly
// services.AddDbContext<AppDbContext>(...);
// services.AddScoped<IEmployeeRepository, EmployeeRepository>();
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L249-L268' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-example' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-logical-example-1'></a>
```cs
/// <summary>
/// Logical mode example - everything runs locally for testing.
/// </summary>
public class LogicalModeExample
{
    [Fact]
    public async Task LogicalMode_AllLocal()
    {
        // Logical mode:
        // - All methods execute locally
        // - No serialization, no HTTP
        // - Ideal for unit testing domain logic

        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "LogicalMode";
        employee.Email = new EmailAddress("logical.mode@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // All operations execute locally
        await factory.Save(employee);

        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("LogicalMode", fetched.FirstName);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L753-L785' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-example-1' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates local vs remote method execution based on [Remote] attribute.
/// </summary>
[Factory]
public partial class EmployeeLocalRemote : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Local execution - no [Remote] attribute.
    /// Runs on client, no network call, no serialization.
    /// </summary>
    [Create]
    public EmployeeLocalRemote()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Remote execution - [Remote] attribute present.
    /// Serialized and sent to server where repository exists.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Remote execution for persistence operations.
    /// Repository only available on server.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L6-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-local-remote-methods' title='Start of snippet'>anchor</a></sup>
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

Configure logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Neatoo.RemoteFactory": "Debug",
      "Neatoo.RemoteFactory.Server": "Debug",
      "Neatoo.RemoteFactory.Client": "Debug",
      "Neatoo.RemoteFactory.Serialization": "Debug"
    }
  }
}
```

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
