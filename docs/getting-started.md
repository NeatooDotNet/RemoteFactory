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
<a id='snippet-getting-started-server-program'></a>
```cs
// Register RemoteFactory services and domain assembly
builder.Services.AddNeatooAspNetCore(
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Register factory types (IEmployeeFactory -> EmployeeFactory)
builder.Services.RegisterMatchingName(typeof(Employee).Assembly);

// Register your infrastructure services
builder.Services.AddInfrastructureServices();

var app = builder.Build();

// Add the /api/neatoo endpoint for remote calls
app.UseNeatoo();
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Program.cs#L11-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Key points:
- `AddNeatooAspNetCore()` registers factories, serialization, and server-side handlers
- `UseNeatoo()` adds the `/api/neatoo` endpoint for remote delegate requests
- Pass assemblies containing factory-enabled types to the registration method

## Client Configuration

Configure RemoteFactory in your Blazor WASM client:

<!-- snippet: getting-started-client-program -->
<a id='snippet-getting-started-client-program'></a>
```cs
// Register RemoteFactory for client mode with domain assembly
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Register HttpClient for remote calls to server
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(serverBaseAddress) });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Program.cs#L14-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Key points:
- `NeatooFactory.Remote` configures client mode (HTTP calls to server)
- Register a keyed HttpClient with `RemoteFactoryServices.HttpClientKey`
- BaseAddress points to your server

## Create Your First Factory-Enabled Class

Define a domain model with factory methods:

<!-- snippet: getting-started-employee-model -->
<a id='snippet-getting-started-employee-model'></a>
```cs
// [Remote] executes on server; [Service] injects from DI (not serialized)
[Remote, Fetch]
public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
{
    var entity = await repo.GetByIdAsync(id, ct);
    if (entity == null) return false;
    MapFromEntity(entity);
    IsNew = false;
    return true;
}

// Save() routes to Insert/Update/Delete based on IsNew and IsDeleted
[Remote, Insert]
public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
{
    await repo.AddAsync(MapToEntity(), ct);
    await repo.SaveChangesAsync(ct);
    IsNew = false;
}

[Remote, Update]
public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
{
    await repo.UpdateAsync(MapToEntity(), ct);
    await repo.SaveChangesAsync(ct);
}

[Remote, Delete]
public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
{
    await repo.DeleteAsync(Id, ct);
    await repo.SaveChangesAsync(ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Aggregates/Employee.cs#L32-L66' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-employee-model' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-getting-started-usage'></a>
```cs
public async Task UseEmployeeFactory(IEmployeeModelFactory factory)
{
    // Create new instance via generated factory
    var employee = factory.Create();
    employee.FirstName = "Jane";
    employee.Email = "jane@example.com";

    // Save routes to Insert (IsNew=true), Update (IsNew=false), or Delete (IsDeleted=true)
    employee = await factory.Save(employee);  // Insert: IsNew becomes false

    // Fetch loads existing data from server
    var fetched = await factory.Fetch(employee!.Id);

    // Modify and save routes to Update
    fetched!.FirstName = "Jane Updated";
    await factory.Save(fetched);

    // Mark deleted and save routes to Delete
    fetched.IsDeleted = true;
    await factory.Save(fetched);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/GettingStarted/EmployeeModelUsage.cs#L5-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-usage' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-getting-started-serialization-config'></a>
```cs
// Ordinal (default): Compact arrays ["Jane","Smith"] - 40-50% smaller
public static NeatooSerializationOptions Ordinal =>
    new() { Format = SerializationFormat.Ordinal };

// Named: Property names {"FirstName":"Jane"} - easier to debug
public static NeatooSerializationOptions Named =>
    new() { Format = SerializationFormat.Named };
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/SerializationConfigSample.cs#L10-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-serialization-config' title='Start of snippet'>anchor</a></sup>
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
