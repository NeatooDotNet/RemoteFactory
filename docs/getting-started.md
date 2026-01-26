# Getting Started

This guide walks through creating your first RemoteFactory application, from installation to a working client-server example.

## Prerequisites

- .NET 8.0, 9.0, or 10.0 SDK
- ASP.NET Core for server (optional - can use Logical mode for single-tier)
- Blazor WebAssembly for client (optional - any .NET client works)

## Project Structure

Typical RemoteFactory solution structure:

```
YourSolution/
├── YourApp.Domain/          # Shared: Domain models with factory methods
├── YourApp.Server/          # ASP.NET Core server
└── YourApp.Client/          # Blazor WASM or other client
```

Domain models are shared across server and client. RemoteFactory generates different code for each based on configuration.

## Installation

### 1. Install NuGet Packages

**Shared domain project:**
```bash
dotnet add package Neatoo.RemoteFactory
```

**Server project:**
```bash
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

The client project doesn't need direct package references - it gets RemoteFactory transitively through the domain project reference.

## Server Configuration

Configure RemoteFactory in your ASP.NET Core server:

<!-- snippet: getting-started-server-program -->
<!--
SNIPPET REQUIREMENTS:
- Show ASP.NET Core Program.cs setup for RemoteFactory server
- Configure NeatooSerializationOptions with SerializationFormat.Ordinal (default, smaller payloads)
- Call services.AddNeatooAspNetCore() with serialization options and assembly containing domain models
- Register IEmployeeRepository as a scoped service (use Employee Management domain)
- Call app.UseNeatoo() to map the /api/neatoo endpoint
- Context: Production server startup code
- Domain: Employee Management (reference EmployeeModel.Assembly)
- Show minimal but complete configuration
-->
<!-- endSnippet -->

Key points:
- `AddNeatooAspNetCore()` registers factories, serialization, and server-side handlers
- `UseNeatoo()` adds the `/api/neatoo` endpoint for remote delegate requests
- Pass assemblies containing factory-enabled types to the registration method

## Client Configuration

Configure RemoteFactory in your Blazor WASM client:

<!-- snippet: getting-started-client-program -->
<!--
SNIPPET REQUIREMENTS:
- Show Blazor WASM Program.cs setup for RemoteFactory client
- Call services.AddNeatooRemoteFactory() with NeatooFactory.Remote and assembly containing domain models
- Register keyed HttpClient using RemoteFactoryServices.HttpClientKey
- Set HttpClient.BaseAddress to the server URL (use parameter or placeholder)
- Context: Production Blazor WASM client startup code
- Domain: Employee Management (reference EmployeeModel.Assembly)
- Show minimal but complete client configuration
-->
<!-- endSnippet -->

Key points:
- `NeatooFactory.Remote` configures client mode (HTTP calls to server)
- Register a keyed HttpClient with `RemoteFactoryServices.HttpClientKey`
- BaseAddress points to your server

## Create Your First Factory-Enabled Class

Define a domain model with factory methods:

<!-- snippet: getting-started-employee-model -->
<!--
SNIPPET REQUIREMENTS:
- Define IEmployeeModel interface extending IFactorySaveMeta
- Include properties: Guid Id, string FirstName, string LastName, string? Email, Guid? DepartmentId, DateTime Created, DateTime Modified
- Add DataAnnotations: [Required] on FirstName and LastName, [EmailAddress] on Email
- Override IsDeleted as settable (new bool IsDeleted { get; set; })

- Define EmployeeModel class with [Factory] attribute, implementing IEmployeeModel
- Make class partial for source generation
- Properties: Id (private set), FirstName, LastName, Email, DepartmentId, Created (private set), Modified (private set), IsNew, IsDeleted

- [Create] constructor: Generate new Guid for Id, set Created/Modified to DateTime.UtcNow

- [Remote][Fetch] method: async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository)
  - Load from repository, map to properties, set IsNew = false, return success bool

- [Remote][Insert] method: async Task Insert([Service] IEmployeeRepository repository)
  - Create entity from properties, add to repository, save, set IsNew = false

- [Remote][Update] method: async Task Update([Service] IEmployeeRepository repository)
  - Load entity, update properties, set Modified, save

- [Remote][Delete] method: async Task Delete([Service] IEmployeeRepository repository)
  - Delete from repository by Id, save

- Context: Production domain model showing complete CRUD lifecycle
- Domain: Employee Management
- Show all RemoteFactory attributes: [Factory], [Create], [Fetch], [Insert], [Update], [Delete], [Remote], [Service]
-->
<!-- endSnippet -->

Attribute breakdown:
- `[Factory]` - Generates `IEmployeeModelFactory` interface and implementation
- `[Create]` - Constructor/method that creates new instances
- `[Fetch]` - Method that loads existing data
- `[Insert]`, `[Update]`, `[Delete]` - Write operations
- `[Remote]` - Executes on server (calls are serialized)
- `[Service]` - Parameter injected from DI container (not serialized)

## Use the Factory

Inject and call the generated factory from your client:

<!-- snippet: getting-started-usage -->
<!--
SNIPPET REQUIREMENTS:
- Show async method demonstrating IEmployeeModelFactory usage
- Method signature: async Task UseEmployeeFactory(IEmployeeModelFactory factory)

- Create: Call factory.Create(), set FirstName, LastName, Email properties
- Insert: Call await factory.Save(employee) - comment notes IsNew = true routes to Insert
- Comment that saved.IsNew is now false

- Fetch: Call await factory.Fetch(saved.Id) to load existing employee

- Update: Modify Email property, call await factory.Save(fetched) - comment notes IsNew = false routes to Update

- Delete: Set fetched.IsDeleted = true, call await factory.Save(fetched) - comment notes IsDeleted = true routes to Delete

- Context: Production client-side usage example
- Domain: Employee Management
- Show complete CRUD lifecycle through factory methods
- Include comments explaining Save routing logic
-->
<!-- endSnippet -->

The generated factory:
- `Create()` calls the `[Create]` constructor
- `Fetch()` serializes the call, sends to `/api/neatoo`, executes on server, returns result
- `Save()` routes to Insert/Update/Delete based on `IFactorySaveMeta` state

## What Just Happened?

When you call `factory.Fetch()`:

1. **Client**: Factory serializes method name and parameters
2. **Client**: HTTP POST to `/api/neatoo` with request payload
3. **Server**: Deserializes request, resolves `IEmployeeRepository` from DI
4. **Server**: Calls `EmployeeModel.Fetch()` with injected service
5. **Server**: Serializes the EmployeeModel instance
6. **Server**: Returns response
7. **Client**: Deserializes EmployeeModel instance

No DTOs. No controllers. No manual mapping.

## Serialization Formats

RemoteFactory supports two serialization formats:

**Ordinal (default):** Compact array format, 40-50% smaller payloads
```json
["John", "Doe", "john@example.com"]
```

**Named:** Verbose format with property names, easier to debug
```json
{"FirstName":"John","LastName":"Doe","Email":"john@example.com"}
```

Configure during registration:

<!-- snippet: getting-started-serialization-config -->
<!--
SNIPPET REQUIREMENTS:
- Define static class SerializationConfig with two methods

- ConfigureOrdinal method:
  - Create NeatooSerializationOptions with Format = SerializationFormat.Ordinal
  - Comment: "Ordinal format (default): Compact array format, 40-50% smaller"
  - Comment showing example payload: ["Jane", "Smith", "jane@example.com"]
  - Call services.AddNeatooAspNetCore(options, typeof(EmployeeModel).Assembly)

- ConfigureNamed method:
  - Create NeatooSerializationOptions with Format = SerializationFormat.Named
  - Comment: "Named format: Verbose with property names, easier to debug"
  - Comment showing example payload: {"FirstName":"Jane","LastName":"Smith","Email":"jane@example.com"}
  - Call services.AddNeatooAspNetCore(options, typeof(EmployeeModel).Assembly)

- Context: Production server configuration showing serialization format options
- Domain: Employee Management
- Show both format options side-by-side for comparison
-->
<!-- endSnippet -->

Both client and server must use the same format.

## Next Steps

- [Factory Operations](factory-operations.md) - Understand all operation types
- [Factory Modes](factory-modes.md) - Configure code generation modes (Full, RemoteOnly, Logical)
- [Service Injection](service-injection.md) - Inject dependencies into factory methods
- [Authorization](authorization.md) - Secure factory operations
- [Save Operation](save-operation.md) - Use IFactorySave for routing
- [Events](events.md) - Fire-and-forget domain events

## Troubleshooting

**Factory interface not generated:**
- Ensure class/interface has `[Factory]` attribute
- Check at least one method has `[Create]`, `[Fetch]`, or other operation attribute
- Clean and rebuild

**Service parameter not resolved:**
- Verify service is registered in DI container
- Check parameter has `[Service]` attribute
- Ensure service is registered in the same container (server-only services won't work on client)

**Serialization errors:**
- Verify both client and server use same serialization format
- Check all properties are serializable (primitives, collections, or types with `[Factory]`)
- Circular references are supported automatically

**401/403 errors:**
- Check authorization configuration matches expected auth flow
- See [Authorization](authorization.md) for securing operations
