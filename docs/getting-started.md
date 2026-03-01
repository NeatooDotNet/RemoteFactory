# Getting Started

In [Why RemoteFactory](the-problem.md) you saw the single-layer concept: write your domain model as if client and server don't exist, and let RemoteFactory handle the persistence routing. This guide makes it real — you'll build an Employee domain class that persists through RemoteFactory without DTOs, controllers, or mapping code.

## Prerequisites

- .NET 8.0, 9.0, or 10.0 SDK
- ASP.NET Core for server (optional — can use Logical mode for single-tier)
- Any .NET client (Blazor WebAssembly, MAUI, WPF, console)

## Project Structure

RemoteFactory's architecture needs three projects: one for the domain model (shared), one for the server, and one for the client. The domain project is referenced by both sides — the same `Employee` class runs on client and server, with RemoteFactory generating different behavior for each.

```
YourSolution/
├── YourApp.Domain/          # Shared: Domain models with factory methods
├── YourApp.Server/          # ASP.NET Core server
└── YourApp.Client/          # Blazor WASM, MAUI, or other client
```

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

The client project doesn't need direct package references — it gets RemoteFactory transitively through the domain project reference.

## Server Configuration

The server registers RemoteFactory, maps the single HTTP endpoint, and registers your infrastructure services (repositories, database contexts, etc.):

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

`AddNeatooAspNetCore()` registers factories, serialization, and server-side handlers. `UseNeatoo()` adds the `/api/neatoo` endpoint — the single entry point for all remote factory calls.

## Client Configuration

The client registers RemoteFactory in Remote mode and provides an HttpClient pointed at the server:

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

`NeatooFactory.Remote` tells RemoteFactory this is the client side — factory calls will be serialized and sent to the server via the keyed HttpClient.

## Define Your Domain Model

This is where RemoteFactory's value shows up. You write the persistence logic directly on the domain object — each attribute tells RemoteFactory which persistence stage the method handles. No separate repository layer, no DTOs, no controllers:

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

Quick reference:
- `[Factory]` — Generates the factory interface and implementation
- `[Create]` — Instantiation (constructors or static methods)
- `[Fetch]` — Load existing data
- `[Insert]`, `[Update]`, `[Delete]` — Write operations (routed by `Save()`)
- `[Remote]` — Client entry point that crosses to the server
- `[Service]` — DI-injected parameter (not serialized)

## Use the Factory

Inject the generated factory and use it. The caller doesn't need to know about client/server — it's just method calls:

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

## What Just Happened?

### The Remote Call

When the client calls `factory.Fetch(id)`:

1. Client factory serializes the method name and value parameters
2. HTTP POST to `/api/neatoo`
3. Server deserializes the request, creates an Employee instance, resolves `IEmployeeRepository` from DI
4. Server calls `Employee.Fetch()` with the injected repository
5. Server serializes the populated Employee and returns it
6. Client deserializes the Employee instance

No DTOs. No controllers. No manual mapping.

### The Persistence Routing

When the client calls `factory.Save(employee)`, RemoteFactory inspects the entity metadata and routes to the right method:

- `IsNew = true` → calls your `[Insert]` method
- `IsNew = false` → calls your `[Update]` method
- `IsDeleted = true` → calls your `[Delete]` method

One `Save()` call, three possible outcomes. You wrote the persistence logic for each case; RemoteFactory decided which one to run.

## Next Steps

- [Factory Operations](factory-operations.md) — All seven persistence operations in detail
- [Service Injection](service-injection.md) — Constructor vs method injection across the boundary
- [Client-Server Architecture](client-server-architecture.md) — The big picture mental model
- [Factory Modes](factory-modes.md) — Remote, Logical, and Server registration modes
- [Save Operation](save-operation.md) — IFactorySave routing in depth

## Troubleshooting

**Factory interface not generated:**
- Ensure class has `[Factory]` attribute
- Check at least one method has `[Create]`, `[Fetch]`, or other operation attribute
- Clean and rebuild

**Service parameter not resolved:**
- Verify service is registered in DI container
- Check parameter has `[Service]` attribute
- Ensure service is registered on the correct side — server-only services won't resolve on the client

**Serialization errors:**
- Verify both client and server use the same serialization format (see [Serialization](serialization.md))
- Check all properties are serializable (primitives, collections, or types with `[Factory]`)

**401/403 errors:**
- Check authorization configuration — see [Authorization](authorization.md)
