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
// Configure RemoteFactory for Server mode with ASP.NET Core integration
var domainAssembly = typeof(Employee).Assembly;

builder.Services.AddNeatooAspNetCore(
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    domainAssembly);

// Register factory types from domain assembly (interfaces to implementations)
builder.Services.RegisterMatchingName(domainAssembly);

// Register infrastructure services (repositories, etc.)
builder.Services.AddInfrastructureServices();

var app = builder.Build();

// Configure the Neatoo RemoteFactory endpoint
app.UseNeatoo();
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Program.cs#L11-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
<a id='snippet-getting-started-server-program-1'></a>
```cs
public static class ServerProgram
{
    public static void ConfigureServer(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure serialization format (Ordinal is default, produces smaller payloads)
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };

        // Register RemoteFactory server-side services with domain assembly
        builder.Services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(EmployeeModel).Assembly);

        // Register server-only services (repositories, etc.)
        builder.Services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();

        var app = builder.Build();

        // Map the /api/neatoo endpoint for remote delegate requests
        app.UseNeatoo();

        app.Run();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/ServerProgramSample.cs#L9-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program-1' title='Start of snippet'>anchor</a></sup>
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
// Configure RemoteFactory for Remote (client) mode
var domainAssembly = typeof(Employee).Assembly;

builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    domainAssembly);

// Register the keyed HttpClient for RemoteFactory remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri(serverBaseAddress) };
});
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Program.cs#L14-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<a id='snippet-getting-started-client-program-1'></a>
```cs
public static class ClientProgram
{
    public static async Task ConfigureClient(string[] args, string serverBaseAddress)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Configure RemoteFactory for Remote (client) mode with domain assembly
        builder.Services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(EmployeeModel).Assembly);

        // Register keyed HttpClient for RemoteFactory remote calls
        builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        {
            return new HttpClient { BaseAddress = new Uri(serverBaseAddress) };
        });

        await builder.Build().RunAsync();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Samples/ClientProgramSample.cs#L7-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program-1' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Employee aggregate root with full CRUD operations.
/// </summary>
[Factory]
public partial class Employee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EmailAddress Email { get; set; } = null!;
    public PhoneNumber? Phone { get; set; }
    public Guid DepartmentId { get; set; }
    public string Position { get; set; } = "";
    public Money Salary { get; set; } = null!;
    public DateTime HireDate { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Employee with a generated ID.
    /// </summary>
    [Create]
    public Employee()
    {
        Id = Guid.NewGuid();
        Salary = new Money(0, "USD");
        HireDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Fetches an existing Employee by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        MapFromEntity(entity);
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Inserts a new Employee into the repository.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    /// <summary>
    /// Updates an existing Employee in the repository.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes the Employee from the repository.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }

    private void MapFromEntity(EmployeeEntity entity)
    {
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = new EmailAddress(entity.Email);
        Phone = entity.Phone != null ? ParsePhone(entity.Phone) : null;
        DepartmentId = entity.DepartmentId;
        Position = entity.Position;
        Salary = new Money(entity.SalaryAmount, entity.SalaryCurrency);
        HireDate = entity.HireDate;
    }

    private EmployeeEntity MapToEntity() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email.Value,
        Phone = Phone?.ToString(),
        DepartmentId = DepartmentId,
        Position = Position,
        SalaryAmount = Salary.Amount,
        SalaryCurrency = Salary.Currency,
        HireDate = HireDate
    };

    private static PhoneNumber ParsePhone(string phone)
    {
        var parts = phone.Split(' ', 2);
        return new PhoneNumber(parts[0], parts.Length > 1 ? parts[1] : "");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Aggregates/Employee.cs#L7-L117' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-employee-model' title='Start of snippet'>anchor</a></sup>
<a id='snippet-getting-started-employee-model-1'></a>
```cs
public interface IEmployeeModel : IFactorySaveMeta
{
    Guid Id { get; }
    [Required] string FirstName { get; set; }
    [Required] string LastName { get; set; }
    [EmailAddress] string? Email { get; set; }
    Guid? DepartmentId { get; set; }
    DateTime Created { get; }
    DateTime Modified { get; }
    new bool IsDeleted { get; set; }
}

[Factory]
public partial class EmployeeModel : IEmployeeModel
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeModel()
    {
        Id = Guid.NewGuid();
        Created = DateTime.UtcNow;
        Modified = DateTime.UtcNow;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        DepartmentId = entity.DepartmentId;
        Created = entity.HireDate;
        Modified = DateTime.UtcNow;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email ?? "",
            DepartmentId = DepartmentId ?? Guid.Empty,
            Position = "",
            SalaryAmount = 0,
            SalaryCurrency = "USD",
            HireDate = Created
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            entity.FirstName = FirstName;
            entity.LastName = LastName;
            entity.Email = Email ?? "";
            entity.DepartmentId = DepartmentId ?? Guid.Empty;
            await repository.UpdateAsync(entity, ct);
            await repository.SaveChangesAsync(ct);
        }
        Modified = DateTime.UtcNow;
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/GettingStarted/EmployeeModel.cs#L7-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-employee-model-1' title='Start of snippet'>anchor</a></sup>
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
    // Create: Call factory.Create() to instantiate a new employee
    var employee = factory.Create();
    employee.FirstName = "Jane";
    employee.LastName = "Smith";
    employee.Email = "jane.smith@example.com";

    // Insert: Save routes to Insert because IsNew = true
    var saved = await factory.Save(employee);
    if (saved == null) return;
    // saved.IsNew is now false

    // Fetch: Load an existing employee by ID
    var fetched = await factory.Fetch(saved.Id);
    if (fetched == null) return;

    // Update: Modify and save routes to Update because IsNew = false
    fetched.Email = "jane.updated@example.com";
    await factory.Save(fetched);

    // Delete: Set IsDeleted = true and save routes to Delete
    fetched.IsDeleted = true;
    await factory.Save(fetched);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/GettingStarted/EmployeeModelUsage.cs#L5-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-usage' title='Start of snippet'>anchor</a></sup>
<a id='snippet-getting-started-usage-1'></a>
```cs
/// <summary>
/// Getting started usage example with factory operations.
/// </summary>
public class GettingStartedUsageTests
{
    [Fact]
    public async Task BasicCrudOperations()
    {
        // Arrange - Create test container
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create - Factory generates IEmployeeFactory from [Factory] attribute
        var employee = factory.Create();
        employee.FirstName = "Alice";
        employee.LastName = "Johnson";
        employee.Email = new EmailAddress("alice.johnson@example.com");
        employee.Position = "Software Engineer";
        employee.Salary = new Money(95000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Insert via Save (routes to Insert based on IsNew = true)
        employee = await factory.Save(employee);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        // Fetch - Load existing employee
        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Alice", fetched.FirstName);

        // Update
        fetched.Position = "Senior Software Engineer";
        fetched.Salary = new Money(115000, "USD");
        fetched = await factory.Save(fetched);

        // Verify update
        var updated = await factory.Fetch(employee.Id);
        Assert.Equal("Senior Software Engineer", updated?.Position);

        // Delete
        fetched.IsDeleted = true;
        await factory.Save(fetched);

        // Verify deletion
        var deleted = await factory.Fetch(employee.Id);
        Assert.Null(deleted);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L15-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-usage-1' title='Start of snippet'>anchor</a></sup>
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
public static class SerializationConfig
{
    public static void ConfigureOrdinal(IServiceCollection services)
    {
        // Ordinal format (default): Compact array format, 40-50% smaller
        // Example payload: ["Jane", "Smith", "jane@example.com"]
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };
        services.AddNeatooAspNetCore(options, typeof(EmployeeModel).Assembly);
    }

    public static void ConfigureNamed(IServiceCollection services)
    {
        // Named format: Verbose with property names, easier to debug
        // Example payload: {"FirstName":"Jane","LastName":"Smith","Email":"jane@example.com"}
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };
        services.AddNeatooAspNetCore(options, typeof(EmployeeModel).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/SerializationConfigSample.cs#L8-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-serialization-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-getting-started-serialization-config-1'></a>
```cs
/// <summary>
/// Demonstrates serialization format configuration.
/// </summary>
public class SerializationConfigTests
{
    [Fact]
    public void OrdinalFormatConfiguration()
    {
        // Ordinal format (default) - compact array-based serialization
        var ordinalOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };
        Assert.Equal(SerializationFormat.Ordinal, ordinalOptions.Format);
    }

    [Fact]
    public void NamedFormatConfiguration()
    {
        // Named format - human-readable JSON with property names
        var namedOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };
        Assert.Equal(SerializationFormat.Named, namedOptions.Format);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L67-L95' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-serialization-config-1' title='Start of snippet'>anchor</a></sup>
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
