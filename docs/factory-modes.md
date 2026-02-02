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
<a id='snippet-modes-full-config-2'></a>
```cs
/// <summary>
/// Configures services for Full mode (server-side).
/// Full mode is the default - no [assembly: FactoryMode] attribute needed.
/// </summary>
public static void ConfigureFullMode(IServiceCollection services)
{
    var domainAssembly = typeof(Employee).Assembly;

    // Full mode is the default (no assembly attribute required)
    // Use NeatooFactory.Server for ASP.NET Core server applications
    services.AddNeatooRemoteFactory(
        NeatooFactory.Server,
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        domainAssembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/FactoryModeConfigurationSamples.cs#L15-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-config-2' title='Start of snippet'>anchor</a></sup>
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
// Conceptual illustration of what the generator produces in Full mode.
// This is a simplified representation - actual generated code is more complex.
//
// public interface IEmployeeFactory
// {
//     IEmployee Create();
//     Task<IEmployee?> Fetch(Guid id);
//     Task Save(IEmployee employee);
// }
//
// public class EmployeeFactory : IEmployeeFactory
// {
//     private readonly IServiceProvider ServiceProvider;
//     private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
//
//     public IEmployee Create() => new Employee();
//
//     public async Task<IEmployee?> Fetch(Guid id)
//     {
//         // Dual execution path based on runtime mode:
//         // - If IMakeRemoteDelegateRequest is registered (Remote mode):
//         //   serialize request, POST to /api/neatoo, deserialize response
//         // - Otherwise (Server/Logical mode):
//         //   execute directly using injected repository
//         if (MakeRemoteDelegateRequest != null)
//             return await callRemoteFetch(id);
//         return await localFetchDelegate(id);
//     }
//
//     public async Task Save(IEmployee employee)
//     {
//         // Same dual-path pattern for save operations
//         if (MakeRemoteDelegateRequest != null)
//             await callRemoteSave(employee);
//         else
//             await localSaveDelegate(employee);
//     }
//
//     // Static method for handling incoming HTTP requests (Server mode)
//     public static void RegisterRemoteDelegates(HandleRemoteDelegateRequest handler)
//     {
//         // Registers handlers for incoming serialized requests
//         handler.Register("Fetch", (payload) => ...);
//         handler.Register("Save", (payload) => ...);
//     }
// }
public static class FullModeGeneratedCodeIllustration
{
    // This class exists only to hold the region for documentation.
    // See the comments above for the conceptual generated code pattern.
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/FactoryModes/GeneratedCodeIllustrations.cs#L6-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-generated' title='Start of snippet'>anchor</a></sup>
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
// RemoteOnly mode assembly attribute
// Client assemblies that only generate HTTP stubs
// Use when you have separate client and server projects
[assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L48-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-remoteonly-config-1'></a>
```cs
// In AssemblyAttributes.cs or GlobalUsings.cs:
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]

/// <summary>
/// Configures services for RemoteOnly mode (client-side).
/// RemoteOnly generates HTTP stubs only - smaller assemblies for clients.
/// </summary>
public static void ConfigureRemoteOnlyMode(IServiceCollection services, string serverUrl)
{
    var domainAssembly = typeof(Employee).Assembly;

    // RemoteOnly mode - all methods make HTTP calls to server
    services.AddNeatooRemoteFactory(
        NeatooFactory.Remote,
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        domainAssembly);

    // Register HttpClient with the key RemoteFactory expects
    services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    {
        return new HttpClient { BaseAddress = new Uri(serverUrl) };
    });
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/FactoryModeConfigurationSamples.cs#L33-L57' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-config-1' title='Start of snippet'>anchor</a></sup>
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
// Conceptual illustration of what the generator produces in RemoteOnly mode.
// No local implementation code - HTTP stubs only.
//
// public class EmployeeFactory : IEmployeeFactory
// {
//     private readonly IServiceProvider ServiceProvider;
//     private readonly IMakeRemoteDelegateRequest MakeRemoteDelegateRequest;
//
//     // Benefits of RemoteOnly mode:
//     // - Smaller assembly size (no entity method code)
//     // - No server dependencies in client bundle
//     // - Clear separation of client and server code
//     // - Faster client startup (less code to load)
//
//     public IEmployee Create() => callRemoteCreate();
//
//     public async Task<IEmployee?> Fetch(Guid id)
//     {
//         // ALL methods serialize and POST to server
//         // No local execution path available
//         return await callRemoteFetch(id);
//     }
//
//     public async Task Save(IEmployee employee)
//     {
//         // No local execution path available
//         await callRemoteSave(employee);
//     }
//
//     // No RegisterRemoteDelegates method
//     // RemoteOnly mode doesn't handle incoming HTTP requests
// }
public static class RemoteOnlyModeGeneratedCodeIllustration
{
    // This class exists only to hold the region for documentation.
    // See the comments above for the conceptual generated code pattern.
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/FactoryModes/GeneratedCodeIllustrations.cs#L60-L98' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-generated' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-modes-server-config-2'></a>
```cs
/// <summary>
/// Configures Server runtime mode with ASP.NET Core integration.
/// AddNeatooAspNetCore internally uses NeatooFactory.Server.
/// </summary>
public static void ConfigureServerMode(IServiceCollection services)
{
    var domainAssembly = typeof(Employee).Assembly;

    // AddNeatooAspNetCore handles incoming HTTP requests and executes locally
    services.AddNeatooAspNetCore(
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        domainAssembly);

    // Register server-side services
    services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/FactoryModes/ServerModeConfigurationSample.cs#L15-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-server-config-2' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-modes-remote-config-2'></a>
```cs
/// <summary>
/// Configures Remote runtime mode for client applications.
/// All factory operations go via HTTP to server.
/// </summary>
public static void ConfigureRemoteMode(IServiceCollection services, string serverUrl)
{
    var domainAssembly = typeof(Employee).Assembly;

    // Remote mode - all factory operations serialize and POST to /api/neatoo
    services.AddNeatooRemoteFactory(
        NeatooFactory.Remote,
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        domainAssembly);

    // Configure HttpClient with server base address
    services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    {
        return new HttpClient { BaseAddress = new Uri(serverUrl) };
    });
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/FactoryModeConfigurationSamples.cs#L59-L80' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remote-config-2' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-modes-logical-config-2'></a>
```cs
/// <summary>
/// Configures Logical runtime mode for single-tier applications or tests.
/// Direct execution, no serialization, no HTTP overhead.
/// </summary>
public static void ConfigureLogicalMode(IServiceCollection services)
{
    var domainAssembly = typeof(Employee).Assembly;

    // Logical mode - executes all methods locally, no HTTP
    services.AddNeatooRemoteFactory(
        NeatooFactory.Logical,
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        domainAssembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/FactoryModeConfigurationSamples.cs#L82-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-config-2' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-modes-logical-testing-1'></a>
```cs
[Fact]
public async Task TestEmployeeCreationWithLogicalMode()
{
    // Test domain logic without HTTP overhead
    var services = new ServiceCollection();

    // Add logging
    services.AddLogging(builder => builder.AddDebug());

    // Add IHostApplicationLifetime (required for event handling)
    services.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();

    var domainAssembly = typeof(Employee).Assembly;

    // Configure Logical mode - direct execution, no serialization
    services.AddNeatooRemoteFactory(
        NeatooFactory.Logical,
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        domainAssembly);

    // Register factory types
    services.RegisterMatchingName(domainAssembly);

    // Register in-memory repository for testing
    services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();

    // Add infrastructure services
    services.AddInfrastructureServices();

    var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();

    // Resolve the factory
    var factory = scope.ServiceProvider.GetRequiredService<IEmployeeFactory>();

    // Create a new employee
    var employee = factory.Create();
    employee.FirstName = "Jane";
    employee.LastName = "Smith";
    employee.Email = new EmailAddress("jane.smith@example.com");

    // Method executes directly, no serialization
    await factory.Save(employee);

    // Fetch the employee to verify persistence
    var fetched = await factory.Fetch(employee.Id);

    // Assert the data was saved correctly
    Assert.NotNull(fetched);
    Assert.Equal("Jane", fetched.FirstName);
    Assert.Equal("Smith", fetched.LastName);
}

private class TestHostLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;
    public void StopApplication() { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/FactoryModes/LogicalModeTestingSample.cs#L18-L79' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-testing-1' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with full CRUD operations for server deployment.
/// </summary>
[Factory]
public partial class EmployeeFullMode : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Employee with generated ID.
    /// </summary>
    [Create]
    public EmployeeFullMode()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an existing Employee by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        DepartmentId = entity.DepartmentId;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Inserts a new Employee.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            DepartmentId = DepartmentId
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    /// <summary>
    /// Updates an existing Employee.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            DepartmentId = DepartmentId
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Server setup with Full mode and Server runtime.
/// </summary>
public static class FullModeServerSetup
{
    public static void Configure(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // AddNeatooAspNetCore uses Server mode - handles incoming HTTP requests
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory types
        services.RegisterMatchingName(domainAssembly);

        // Register server-side repositories
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/FactoryModes/FullModeServerExample.cs#L10-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-example-1' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-full-example-2'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L719-L751' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-example-2' title='Start of snippet'>anchor</a></sup>
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
/// Client-side state service (client-only dependency).
/// </summary>
public interface IClientStateService
{
    Guid CurrentUserId { get; }
    void SetCurrentEmployeeId(Guid employeeId);
}

/// <summary>
/// Default implementation of client state service.
/// </summary>
public class ClientStateService : IClientStateService
{
    public Guid CurrentUserId { get; } = Guid.NewGuid();
    private Guid _currentEmployeeId;

    public void SetCurrentEmployeeId(Guid employeeId)
    {
        _currentEmployeeId = employeeId;
    }
}

/// <summary>
/// Client setup with RemoteOnly mode and Remote runtime.
/// </summary>
public static class RemoteOnlyModeClientSetup
{
    public static void Configure(IServiceCollection services, string serverUrl)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Remote mode - all operations serialize and POST to server
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory types
        services.RegisterMatchingName(domainAssembly);

        // Configure HttpClient with server address and timeout
        services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        {
            return new HttpClient
            {
                BaseAddress = new Uri(serverUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        });

        // Register client-only services
        services.AddSingleton<IClientStateService, ClientStateService>();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/CompleteSetupExamples.cs#L11-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-example-1' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-remoteonly-example-2'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L787-L810' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-example-2' title='Start of snippet'>anchor</a></sup>
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
/// Logical mode setup for single-tier applications.
/// </summary>
public static class LogicalModeSetup
{
    public static void Configure(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Logical mode - direct local execution, no HTTP
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory types
        services.RegisterMatchingName(domainAssembly);

        // Register repositories locally
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IDepartmentRepository, InMemoryDepartmentRepository>();
    }
}

/// <summary>
/// Demonstrates single-tier application using Logical mode.
/// </summary>
public static class SingleTierAppExample
{
    public static async Task RunLocally()
    {
        // Build the service container
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add IHostApplicationLifetime (required for some features)
        services.AddSingleton<IHostApplicationLifetime, SingleTierHostLifetime>();

        // Configure Logical mode
        LogicalModeSetup.Configure(services);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Resolve the factory
        var factory = scope.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create a new employee
        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";

        // Executes directly - no HTTP, no serialization
        await factory.Save(employee);

        // Fetch the employee back
        var fetched = await factory.Fetch(employee.Id);

        // Verify the data persisted
        System.Diagnostics.Debug.Assert(fetched != null);
        System.Diagnostics.Debug.Assert(fetched.FirstName == "John");
    }

    private class SingleTierHostLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/CompleteSetupExamples.cs#L69-L143' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-example-1' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-logical-example-2'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L753-L785' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-example-2' title='Start of snippet'>anchor</a></sup>
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
/// Employee entity demonstrating mixed local and remote method execution.
/// </summary>
[Factory]
public partial class EmployeeModeDemo : IEmployeeModeDemo
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string LocalComputedValue { get; private set; } = "";
    public string? ServerLoadedData { get; private set; }

    [Create]
    public EmployeeModeDemo()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Local-only method - executes on client/server directly.
    /// No [Remote] attribute means this never goes over HTTP.
    /// </summary>
    [Fetch]
    public void FetchLocalComputed(string computedInput)
    {
        // This method runs locally regardless of mode
        // Use for client-side calculations or local data
        LocalComputedValue = $"Computed: {computedInput}";
    }

    /// <summary>
    /// Remote method - serializes and executes on server.
    /// The [Remote] attribute means this can be called over HTTP.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> FetchFromServer(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        // This method executes on server (or locally in Logical mode)
        // Server-only services are injected via [Service] attribute
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        ServerLoadedData = $"Loaded from server at {DateTime.UtcNow:O}";
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/FactoryModes/GeneratedCodeIllustrations.cs#L112-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-local-remote-methods' title='Start of snippet'>anchor</a></sup>
<a id='snippet-modes-local-remote-methods-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs#L6-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-local-remote-methods-1' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Configures factory with verbose logging for debugging.
/// </summary>
public static void ConfigureWithLogging(IServiceCollection services, NeatooFactory mode)
{
    // Configure detailed logging
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
    });
    // Logs show:
    // - "Executing local factory method..." for Server/Logical modes
    // - "Sending remote factory request..." for Remote mode
    // - Serialization format and payload size

    var domainAssembly = typeof(Employee).Assembly;

    services.AddNeatooRemoteFactory(
        mode,
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        domainAssembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/FactoryModeConfigurationSamples.cs#L99-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logging' title='Start of snippet'>anchor</a></sup>
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
