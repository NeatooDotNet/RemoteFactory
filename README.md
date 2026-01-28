# Neatoo RemoteFactory

**Roslyn Source Generator-powered Data Mapper Factory for 3-tier .NET applications**

RemoteFactory eliminates DTOs, manual factories, and API controllers by generating everything at compile time. Write domain model methods once, get client and server implementations automatically.

## Why RemoteFactory?

**Traditional 3-tier architecture:**
- Write domain model methods
- Create DTOs to transfer state
- Build factories to map between DTOs and domain models
- Write API controllers to expose operations
- Maintain all four layers as requirements change

**With RemoteFactory:**
- Write domain model methods
- Add attributes (`[Factory]`, `[Remote]`, `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, etc.)
- Done. Generator creates factories, serialization, and endpoints.

## Quick Example

Domain model with factory methods:

<!-- snippet: readme-domain-model -->
<a id='snippet-readme-domain-model'></a>
```cs
public interface IPerson : IFactorySaveMeta
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string? Email { get; set; }
    new bool IsDeleted { get; set; }
}

[Factory]
public partial class Person : IPerson
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public Person()
    {
        Id = Guid.NewGuid();
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
            Created = DateTime.UtcNow,
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
        entity.Modified = DateTime.UtcNow;

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
<sup><a href='/src/docs/samples/ReadmeSamples.cs#L13-L96' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-domain-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Client code calls the factory:

<!-- snippet: readme-client-usage -->
<a id='snippet-readme-client-usage'></a>
```cs
// Create a new person
// var person = factory.Create();
// person.FirstName = "John";
// person.LastName = "Doe";
// person.Email = "john.doe@example.com";
//
// // Save routes to Insert (IsNew = true)
// var saved = await factory.Save(person);
//
// // Fetch an existing person
// var fetched = await factory.Fetch(saved!.Id);
// // fetched.FirstName is "John"
//
// // Update - Save routes to Update (IsNew = false)
// fetched!.Email = "john.updated@example.com";
// await factory.Save(fetched);
//
// // Delete - set IsDeleted, then Save
// fetched.IsDeleted = true;
// await factory.Save(fetched);
```
<sup><a href='/src/docs/samples/ReadmeSamples.cs#L98-L119' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-client-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Server automatically exposes the endpoint at `/api/neatoo`. No controllers needed.

## Key Features

- **Zero boilerplate**: No DTOs, no manual mapping, no controllers
- **Type-safe**: Roslyn generates strongly-typed factories from your domain methods
- **DI integration**: Inject services into factory methods with `[Service]` attribute
- **Authorization**: Built-in support for custom auth or ASP.NET Core policies
- **Compact serialization**: Ordinal format reduces payloads by 40-50%
- **Lifecycle hooks**: `IFactoryOnStart`, `IFactoryOnComplete`, `IFactoryOnCancelled`
- **Fire-and-forget events**: Domain events with scope isolation via `[Event]` attribute
- **Flexible modes**: Full (server), RemoteOnly (client), or Logical (single-tier)

## Installation

Install NuGet packages:

**Server project:**
```bash
dotnet add package Neatoo.RemoteFactory
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

**Client project (Blazor WASM, etc.):**
```bash
dotnet add package Neatoo.RemoteFactory
```

**Shared project (domain models):**
```bash
dotnet add package Neatoo.RemoteFactory
```

Configure client assembly for smaller output:

<!-- snippet: readme-client-assembly-mode -->
<a id='snippet-readme-client-assembly-mode'></a>
```cs
// In client assembly's AssemblyAttributes.cs:
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]
```
<sup><a href='/src/docs/samples/ReadmeSamples.cs#L153-L156' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-client-assembly-mode' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Getting Started

**Server setup (ASP.NET Core):**

<!-- snippet: readme-server-setup -->
<a id='snippet-readme-server-setup'></a>
```cs
public static class ServerSetup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register Neatoo ASP.NET Core services
        // services.AddNeatooAspNetCore(typeof(Person).Assembly);

        // Register domain services
        // services.AddScoped<IPersonRepository, PersonRepository>();
    }
}
```
<sup><a href='/src/docs/samples/ReadmeSamples.cs#L121-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-server-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Client setup (Blazor WASM):**

<!-- snippet: readme-client-setup -->
<a id='snippet-readme-client-setup'></a>
```cs
public static class ClientSetup
{
    public static void ConfigureServices(IServiceCollection services, Uri baseAddress)
    {
        // Register Neatoo RemoteFactory for client
        // services.AddNeatooRemoteFactory(
        //     NeatooFactory.Remote,
        //     typeof(Person).Assembly);

        // Register keyed HttpClient for Neatoo
        // services.AddKeyedScoped(
        //     RemoteFactoryServices.HttpClientKey,
        //     (sp, key) => new HttpClient { BaseAddress = baseAddress });
    }
}
```
<sup><a href='/src/docs/samples/ReadmeSamples.cs#L135-L151' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-client-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Domain model:**

<!-- snippet: readme-full-example -->
<a id='snippet-readme-full-example'></a>
```cs
[Factory]
[AuthorizeFactory<IPersonAuthorization>]
public partial class PersonWithAuth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PersonWithAuth()
    {
        Id = Guid.NewGuid();
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
            Created = DateTime.UtcNow,
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
        entity.Modified = DateTime.UtcNow;

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

public interface IPersonAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

public class PersonAuthorization : IPersonAuthorization
{
    private readonly IUserContext _userContext;

    public PersonAuthorization(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreate() => _userContext.IsAuthenticated;
    public bool CanRead() => _userContext.IsAuthenticated;
    public bool CanWrite() =>
        _userContext.IsInRole("Admin") || _userContext.IsInRole("Manager");
}
```
<sup><a href='/src/docs/samples/ReadmeSamples.cs#L158-L260' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-full-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Getting Started](docs/getting-started.md) for a complete walkthrough.

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and first example
- [Factory Operations](docs/factory-operations.md) - Create, Fetch, Insert, Update, Delete, Execute, Event
- [Service Injection](docs/service-injection.md) - DI integration with `[Service]` attribute
- [Authorization](docs/authorization.md) - Custom auth and ASP.NET Core policies
- [Serialization](docs/serialization.md) - Ordinal vs Named formats
- [Save Operation](docs/save-operation.md) - IFactorySave routing pattern
- [Factory Modes](docs/factory-modes.md) - Full, RemoteOnly, Logical
- [Events](docs/events.md) - Fire-and-forget domain events
- [ASP.NET Core Integration](docs/aspnetcore-integration.md) - Server-side configuration
- [Attributes Reference](docs/attributes-reference.md) - All available attributes
- [Interfaces Reference](docs/interfaces-reference.md) - All available interfaces

## Supported Frameworks

- .NET 8.0 (LTS)
- .NET 9.0 (STS)
- .NET 10.0 (LTS)

## Examples

Complete working examples in `src/Examples/`:

- **Person** - Simple Blazor WASM CRUD application
- **OrderEntry** - Order entry system with aggregate roots

## License

MIT License - see [LICENSE](LICENSE) for details.

## Links

- [NuGet: Neatoo.RemoteFactory](https://nuget.org/packages/Neatoo.RemoteFactory)
- [NuGet: Neatoo.RemoteFactory.AspNetCore](https://nuget.org/packages/Neatoo.RemoteFactory.AspNetCore)
- [Release Notes](docs/release-notes/index.md)
