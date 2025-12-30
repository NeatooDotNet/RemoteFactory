# Neatoo RemoteFactory

> A Roslyn Source Generator-powered Data Mapper Factory for 3-tier .NET applications.
> Build client/server applications without writing DTOs, factories, or API controllers.

[![NuGet](https://img.shields.io/nuget/v/Neatoo.RemoteFactory)](https://www.nuget.org/packages/Neatoo.RemoteFactory)
[![Discord](https://img.shields.io/discord/your-discord-id)](https://discord.gg/M3dVuZkG)

## Why RemoteFactory?

Traditional 3-tier architectures require extensive boilerplate: DTOs that mirror your domain models, controllers for each entity, manual mapping code, and scattered authorization logic. RemoteFactory eliminates all of this.

**Before RemoteFactory:**
- Write DTO classes that duplicate domain model properties
- Write AutoMapper profiles or manual mapping code
- Write API controllers for each entity
- Write service layer factories
- Maintain synchronization between all these layers

**With RemoteFactory:**
- Annotate your domain model with `[Factory]`
- Add operation methods with `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`
- Everything else is generated at compile time

## Quick Example

```csharp
[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public class PersonModel : IPersonModel
{
    [Create]
    public PersonModel()
    {
        Created = DateTime.Now;
    }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // This runs on the server - [Service] parameters are injected
    [Remote]
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonContext context)
    {
        var entity = await context.Persons.FirstOrDefaultAsync();
        if (entity == null) return false;
        this.FirstName = entity.FirstName;
        this.LastName = entity.LastName;
        this.IsNew = false;
        return true;
    }

    [Remote]
    [Insert]
    [Update]
    public async Task Save([Service] IPersonContext context)
    {
        // Upsert logic here
    }
}
```

RemoteFactory generates a complete factory interface:

```csharp
public interface IPersonModelFactory
{
    IPersonModel? Create();
    Task<IPersonModel?> Fetch();
    Task<IPersonModel?> Save(IPersonModel target);
    Task<Authorized<IPersonModel>> TrySave(IPersonModel target);
    Authorized CanCreate();
    Authorized CanFetch();
    Authorized CanSave();
}
```

## Key Features

- **Zero DTOs**: Domain objects serialize directly between client and server
- **Single Endpoint**: One controller handles all operations via `/api/neatoo`
- **Generated Factories**: Full CRUD with automatic DI registration
- **Built-in Authorization**: Declarative access control with generated `Can*` methods
- **Source Generators**: Compile-time generation, no runtime reflection
- **Async/Await Centric**: First-class async support throughout

## Getting Started

### 1. Install NuGet Packages

```bash
# Domain model and client projects
dotnet add package Neatoo.RemoteFactory

# ASP.NET Core server projects
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

### 2. Configure the Server

```csharp
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register RemoteFactory services with your domain model assembly
builder.Services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);
builder.Services.AddScoped<IPersonContext, PersonContext>();

var app = builder.Build();

// Add the RemoteFactory endpoint
app.UseNeatoo();

app.Run();
```

### 3. Configure the Client

```csharp
using Neatoo.RemoteFactory;

// Blazor WebAssembly
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://your-server/") };
});
```

### 4. Use the Factory

```csharp
public class PersonComponent
{
    private readonly IPersonModelFactory _factory;

    public PersonComponent(IPersonModelFactory factory)
    {
        _factory = factory;
    }

    public async Task LoadAndSave()
    {
        // Check authorization before showing UI
        if (!_factory.CanFetch().HasAccess) return;

        // Fetch from server
        var person = await _factory.Fetch();

        // Modify
        person.FirstName = "Updated";

        // Save with authorization handling
        var result = await _factory.TrySave(person);
        if (!result.HasAccess)
        {
            Console.WriteLine($"Save failed: {result.Message}");
        }
    }
}
```

## Documentation

Comprehensive documentation is available in the [docs folder](docs/index.md):

- [Installation Guide](docs/getting-started/installation.md)
- [Quick Start Tutorial](docs/getting-started/quick-start.md)
- [Architecture Overview](docs/concepts/architecture-overview.md)
- [Factory Operations](docs/concepts/factory-operations.md)
- [Authorization](docs/authorization/authorization-overview.md)
- [Source Generation](docs/source-generation/how-it-works.md)
- [Complete Attribute Reference](docs/reference/attributes.md)

## Framework Comparison

| Feature | RemoteFactory | CSLA | Manual DTOs |
|---------|--------------|------|-------------|
| Code Generation | Roslyn Source Generators | Runtime + some codegen | None |
| Base Class Required | No | Yes (BusinessBase, etc.) | No |
| DTO Classes | Not needed | Built-in serialization | Required |
| Learning Curve | Low | Medium-High | Low |
| Boilerplate | Minimal | Low | High |
| 3-Tier Support | Yes | Yes | Manual |
| Authorization | Attribute-based | Method-based | Manual |

See the [comparison documentation](docs/comparison/overview.md) for detailed analysis.

## Examples

### Person Demo

The [Person Demo](src/Examples/Person) shows a complete Blazor WebAssembly application with:
- Domain model with all operations
- Authorization rules
- Entity Framework Core integration
- Generated factory usage

![Person Demo](https://raw.githubusercontent.com/NeatooDotNet/RemoteFactory/main/RemoteFactory%20Person.gif)

### Running the Example

```bash
# Clone the repository
git clone https://github.com/NeatooDotNet/RemoteFactory.git

# Navigate to the solution
cd RemoteFactory

# Run EF migrations
cd src/Examples/Person/Person.Ef
dotnet ef database update

# Start the server (in one terminal)
cd ../Person.Server
dotnet run

# Start the client (in another terminal)
cd ../PersonApp
dotnet run
```

## Video Introduction

[![Introduction](https://raw.githubusercontent.com/NeatooDotNet/RemoteFactory/main/youtubetile.jpg)](https://youtu.be/e9zZ6d8LKkM?si=-EcUFep7Gih-7GiM)

## Community

- [Discord](https://discord.gg/M3dVuZkG) - Ask questions, share projects, get help
- [GitHub Issues](https://github.com/NeatooDotNet/RemoteFactory/issues) - Report bugs, request features

## Related Projects

- [Neatoo](https://github.com/NeatooDotNet/Neatoo) - Rich Domain Model framework with business rules, validation, and data-binding

## Contributing

Contributions are welcome! Please see our contributing guidelines for details.

## License

RemoteFactory is released under the [MIT License](LICENSE).
