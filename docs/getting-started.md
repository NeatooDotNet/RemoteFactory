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
// Server Program.cs
public static class GettingStartedServerProgram
{
    public static void ConfigureServer(IServiceCollection services)
    {
        // Add Neatoo ASP.NET Core with custom serialization
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal // Compact format (default)
        };

        services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(GettingStartedSamples.PersonModel).Assembly);

        // Register domain services
        services.AddScoped<IPersonRepository, PersonRepository>();

        // Add CORS for Blazor client
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://localhost:5001")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
    }

    public static void ConfigureApp(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.UseCors();
        app.UseNeatoo(); // Maps /api/neatoo endpoint
    }
}
```
<sup><a href='/src/docs/samples/GettingStartedSamples.cs#L201-L238' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
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
// Client Program.cs (Blazor WASM)
public static class GettingStartedClientProgram
{
    public static void ConfigureClient(IServiceCollection services, string serverUrl)
    {
        // Register Neatoo RemoteFactory for remote mode
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(GettingStartedSamples.PersonModel).Assembly);

        // Register keyed HttpClient for Neatoo API calls
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient
            {
                BaseAddress = new Uri(serverUrl)
            });
    }
}
```
<sup><a href='/src/docs/samples/GettingStartedSamples.cs#L240-L260' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Key points:
- `NeatooFactory.Remote` configures client mode (HTTP calls to server)
- Register a keyed HttpClient with `RemoteFactoryServices.HttpClientKey`
- BaseAddress points to your server

## Create Your First Factory-Enabled Class

Define a domain model with factory methods:

<!-- snippet: getting-started-person-model -->
<a id='snippet-getting-started-person-model'></a>
```cs
public interface IPersonModel : IFactorySaveMeta
{
    Guid Id { get; }

    [Required(ErrorMessage = "First name is required")]
    string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    string LastName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    string? Email { get; set; }

    string? Phone { get; set; }
    DateTime Created { get; }
    DateTime Modified { get; }

    // Override IsDeleted to make it settable for deletion support
    new bool IsDeleted { get; set; }
}

[Factory]
public partial class PersonModel : IPersonModel
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime Created { get; private set; }
    public DateTime Modified { get; private set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PersonModel()
    {
        Id = Guid.NewGuid();
        Created = DateTime.UtcNow;
        Modified = DateTime.UtcNow;
    }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        Phone = entity.Phone;
        Created = entity.Created;
        Modified = entity.Modified;
        IsNew = false;
        return true;
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IPersonRepository repository)
    {
        var entity = new PersonEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Phone = Phone,
            Created = Created,
            Modified = DateTime.UtcNow
        };
        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
        IsNew = false;
    }

    [Remote]
    [Update]
    public async Task Update([Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id)
            ?? throw new InvalidOperationException($"Person {Id} not found");

        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.Email = Email;
        entity.Phone = Phone;
        entity.Modified = DateTime.UtcNow;
        Modified = entity.Modified;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonRepository repository)
    {
        await repository.DeleteAsync(Id);
        await repository.SaveChangesAsync();
    }
}
```
<sup><a href='/src/docs/samples/GettingStartedSamples.cs#L14-L120' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-person-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Attribute breakdown:
- `[Factory]` - Generates `IPersonFactory` interface and implementation
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
public async Task UsePersonFactory(IPersonModelFactory factory)
{
    // Create new person
    var person = factory.Create();
    person.FirstName = "Jane";
    person.LastName = "Smith";
    person.Email = "jane.smith@example.com";
    person.Phone = "555-0123";

    // Save (Insert) - routes to Insert method when IsNew=true
    var saved = await factory.Save(person);

    // Fetch existing person by ID
    var fetched = await factory.Fetch(saved!.Id);

    // Update - routes to Update method when IsNew=false
    fetched!.Email = "jane.updated@example.com";
    var updated = await factory.Save(fetched);

    // Delete - routes to Delete method when IsDeleted=true
    updated!.IsDeleted = true;
    await factory.Save(updated);
}
```
<sup><a href='/src/docs/samples/GettingStartedSamples.cs#L122-L146' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generated factory:
- `Create()` calls the `[Create]` constructor
- `Fetch()` serializes the call, sends to `/api/neatoo`, executes on server, returns result
- `Save()` routes to Insert/Update/Delete based on `IFactorySaveMeta` state

## What Just Happened?

When you call `factory.Fetch()`:

1. **Client**: Factory serializes method name and parameters
2. **Client**: HTTP POST to `/api/neatoo` with request payload
3. **Server**: Deserializes request, resolves `IPersonRepository` from DI
4. **Server**: Calls `PersonModel.Fetch()` with injected service
5. **Server**: Serializes the PersonModel instance
6. **Server**: Returns response
7. **Client**: Deserializes PersonModel instance

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
public static class SerializationConfigExample
{
    public static void ConfigureWithNamedFormat(IServiceCollection services)
    {
        // Use Named format for easier debugging (larger payloads)
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(GettingStartedSamples.PersonModel).Assembly);
    }

    public static void ConfigureWithOrdinalFormat(IServiceCollection services)
    {
        // Use Ordinal format for production (compact payloads)
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal // Default
        };

        services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(GettingStartedSamples.PersonModel).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/GettingStartedSamples.cs#L262-L291' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-serialization-config' title='Start of snippet'>anchor</a></sup>
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
