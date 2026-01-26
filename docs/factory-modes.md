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
<!--
SNIPPET REQUIREMENTS:
- Show a static configuration class with a method that configures services for Full mode
- Use AddNeatooRemoteFactory with NeatooFactory.Server and pass the assembly
- Include a comment noting Full mode is the default (no attribute needed)
- Context: Server-side ASP.NET Core configuration
- Domain: Employee Management (use EmployeeManagement assembly reference)
-->
<!-- endSnippet -->

### Generated Code

Full mode generates:
- Local methods that call entity methods directly
- Remote delegates that handle incoming HTTP requests
- Serialization converters
- Factory interface and implementation

<!-- snippet: modes-full-generated -->
<!--
SNIPPET REQUIREMENTS:
- Show a simplified representation of what the generator produces in Full mode
- Demonstrate the dual execution path: local vs remote based on IMakeRemoteDelegateRequest presence
- Show IEmployeeFactory interface and EmployeeFactory implementation structure
- Include both Create() and Fetch() methods to illustrate the pattern
- Use comments to explain the conditional logic
- Context: Generated code illustration (can use pseudo-code style with comments)
- Domain: Employee Management (IEmployeeFactory, Employee entity)
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a static configuration class for client-side RemoteOnly mode setup
- Include a comment showing the assembly attribute: [assembly: FactoryMode(FactoryMode.RemoteOnly)]
- Use AddNeatooRemoteFactory with NeatooFactory.Remote
- Register HttpClient with RemoteFactoryServices.HttpClientKey using AddKeyedScoped
- Set BaseAddress from the serverUrl parameter
- Context: Client-side (Blazor WASM or similar) configuration
- Domain: Employee Management assembly reference
-->
<!-- endSnippet -->

Place in `AssemblyAttributes.cs` or `GlobalUsings.cs`.

### Generated Code

RemoteOnly mode generates:
- HTTP call stubs that serialize and POST to `/api/neatoo`
- Serialization converters
- Factory interface (no local implementation)

<!-- snippet: modes-remoteonly-generated -->
<!--
SNIPPET REQUIREMENTS:
- Show a simplified representation of what the generator produces in RemoteOnly mode
- Demonstrate that ALL methods go through IMakeRemoteDelegateRequest (no local path)
- Show IEmployeeFactory interface and EmployeeFactory implementation structure
- Include both Create() and Fetch() methods showing only remote calls
- Add comments listing benefits: smaller assembly, no server dependencies, clear separation
- Context: Generated code illustration (can use pseudo-code style with comments)
- Domain: Employee Management (IEmployeeFactory, Employee entity)
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a static configuration class for Server runtime mode
- Use AddNeatooAspNetCore which internally uses NeatooFactory.Server
- Register server-side services: IEmployeeRepository with EmployeeRepository implementation
- Include comment explaining AddNeatooAspNetCore handles incoming HTTP requests
- Context: ASP.NET Core server Program.cs or Startup configuration
- Domain: Employee Management (IEmployeeRepository, EmployeeRepository)
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a static configuration class for Remote runtime mode
- Use AddNeatooRemoteFactory with NeatooFactory.Remote
- Register HttpClient with RemoteFactoryServices.HttpClientKey using AddKeyedScoped
- Set BaseAddress from the serverUrl parameter
- Include comment: all factory operations go via HTTP to server
- Context: Client-side DI configuration (Blazor, MAUI, desktop)
- Domain: Employee Management assembly reference
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a static configuration class for Logical runtime mode
- Use AddNeatooRemoteFactory with NeatooFactory.Logical
- Include comment: direct execution, no serialization, for single-tier apps or tests
- Context: Console app, background service, or test configuration
- Domain: Employee Management assembly reference
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a complete test method demonstrating Logical mode for testing
- Build a ServiceCollection with AddLogging and IHostApplicationLifetime
- Configure AddNeatooRemoteFactory with NeatooFactory.Logical
- Register IEmployeeRepository with an in-memory or mock implementation
- Build provider, create scope, resolve IEmployeeFactory
- Call factory.Create() and set properties (FirstName, LastName)
- Call factory.Fetch() to demonstrate direct execution without HTTP
- Include comments: "Test domain logic without HTTP overhead", "Method executes directly, no serialization"
- Context: Unit/integration test class
- Domain: Employee Management (IEmployeeFactory, Employee, IEmployeeRepository)
-->
<!-- endSnippet -->

Tests execute locally without HTTP server or serialization overhead.

## Complete Examples

### Server Setup (Full + Server):

<!-- snippet: modes-full-example -->
<!--
SNIPPET REQUIREMENTS:
- Show a complete Employee entity with [Factory] attribute implementing IFactorySaveMeta
- Properties: Id (Guid), FirstName, LastName, DepartmentId, IsNew, IsDeleted
- [Create] constructor that generates new Guid
- [Remote, Fetch] method that loads from IEmployeeRepository
- [Remote, Insert] method that adds to repository and saves
- [Remote, Update] method that updates existing and saves
- Also show a static FullModeServerSetup class with Configure method
- Configure method uses AddNeatooAspNetCore and registers IEmployeeRepository
- Context: Complete server-side domain entity and configuration
- Domain: Employee Management (Employee entity, IEmployeeRepository)
-->
<!-- endSnippet -->

### Client Setup (RemoteOnly + Remote):

<!-- snippet: modes-remoteonly-example -->
<!--
SNIPPET REQUIREMENTS:
- Show a complete client-side setup class for RemoteOnly mode
- Static Configure method taking IServiceCollection and serverUrl
- Use AddNeatooRemoteFactory with NeatooFactory.Remote
- Register HttpClient with RemoteFactoryServices.HttpClientKey, BaseAddress, and Timeout (30 seconds)
- Register a client-side service (IClientStateService or similar) to show client-only dependencies
- Include the IClientStateService interface and implementation
- Context: Complete client-side (Blazor WASM) configuration
- Domain: Employee Management assembly reference, client-side service
-->
<!-- endSnippet -->

### Single-Tier Setup (Full + Logical):

<!-- snippet: modes-logical-example -->
<!--
SNIPPET REQUIREMENTS:
- Show a complete single-tier setup with Logical mode
- Static LogicalModeSetup.Configure method using AddNeatooRemoteFactory with NeatooFactory.Logical
- Register IEmployeeRepository and IDepartmentRepository locally
- Show a SingleTierAppExample class with a RunLocally method
- Build ServiceCollection, add logging, add IHostApplicationLifetime
- Call LogicalModeSetup.Configure, build provider, create scope
- Resolve IEmployeeFactory, call Create(), set FirstName and LastName
- Include assertions and comment: "Executes directly - no HTTP, no serialization"
- Context: Console app or single-tier application example
- Domain: Employee Management (IEmployeeFactory, Employee, IEmployeeRepository, IDepartmentRepository)
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show an Employee entity demonstrating mixed local and remote methods
- Properties: Id (Guid), FirstName, LastName, LocalComputedValue, ServerLoadedData
- [Create] constructor generating new Guid
- [Fetch] WITHOUT [Remote] - local-only method that sets LocalComputedValue from parameter
- Include comment: "Local-only method - executes on client/server directly, no [Remote] attribute"
- [Remote, Fetch] method that loads from IEmployeeRepository and sets ServerLoadedData
- Include comment: "Remote method - serializes and executes on server"
- Context: Domain entity showing method execution behavior differences
- Domain: Employee Management (Employee, IEmployeeRepository)
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a static ModeLogging class with ConfigureWithLogging method
- Method takes IServiceCollection and NeatooFactory mode parameter
- Configure logging with AddLogging: AddConsole, SetMinimumLevel(Debug), AddFilter for Neatoo.RemoteFactory at Trace level
- Call AddNeatooRemoteFactory with the provided mode
- Include trailing comments describing what logs show:
  - "Executing local factory method..." for Server/Logical modes
  - "Sending remote factory request..." for Remote mode
  - Serialization format and payload size
- Context: Production debugging/diagnostics configuration
- Domain: Employee Management assembly reference
-->
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
