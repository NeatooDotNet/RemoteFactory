---
layout: default
title: "Neatoo RemoteFactory Documentation"
description: "A Roslyn Source Generator-powered Data Mapper Factory for 3-tier .NET applications"
---

# Neatoo RemoteFactory

**A Roslyn Source Generator-powered Data Mapper Factory for 3-tier .NET applications.**

Build client/server applications without writing DTOs, factories, or API controllers. RemoteFactory generates everything you need at compile time, providing a seamless bridge between your UI and server with zero boilerplate.

## What is RemoteFactory?

RemoteFactory is a code generation framework that eliminates the repetitive infrastructure code typically required in 3-tier architectures. By analyzing your domain model classes at compile time, it automatically generates:

- **Factory interfaces and implementations** for CRUD operations
- **Delegate types** for remote method invocation
- **DI registrations** for all generated components

## Supported Frameworks

| Framework | Support |
|-----------|---------|
| .NET 8.0 | LTS (Long Term Support) |
| .NET 9.0 | STS (Standard Term Support) |
| .NET 10.0 | LTS (Long Term Support) |

All three frameworks are included in the NuGet packages.

## Key Benefits

- **Zero DTOs Required**: Your domain objects serialize directly between client and server
- **Single API Endpoint**: One controller handles all operations via generated delegates
- **Compile-Time Generation**: No runtime reflection, full IntelliSense support, and compile-time safety
- **Built-in Authorization**: Declarative access control with generated `Can*` methods
- **Full DI Support**: All factories and services integrate with Microsoft.Extensions.DependencyInjection
- **Flexible Execution Modes**: Server, Remote, and Logical modes for different deployment scenarios

## Quick Example

Define your domain model with a few attributes:

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

    [Remote]
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonContext personContext)
    {
        var entity = await personContext.Persons.FirstOrDefaultAsync();
        if (entity == null) return false;
        this.FirstName = entity.FirstName;
        this.LastName = entity.LastName;
        this.IsNew = false;
        return true;
    }

    [Remote]
    [Insert]
    [Update]
    public async Task Upsert([Service] IPersonContext personContext)
    {
        var entity = await personContext.Persons.FirstOrDefaultAsync()
            ?? new PersonEntity();
        entity.FirstName = this.FirstName;
        entity.LastName = this.LastName;
        await personContext.SaveChangesAsync();
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

Use the factory in your application:

```csharp
public class PersonComponent
{
    private readonly IPersonModelFactory _factory;

    public PersonComponent(IPersonModelFactory factory)
    {
        _factory = factory;
    }

    public async Task LoadPerson()
    {
        if (_factory.CanFetch().HasAccess)
        {
            var person = await _factory.Fetch();
            // Use the person model
        }
    }

    public async Task SavePerson(IPersonModel person)
    {
        var result = await _factory.TrySave(person);
        if (result.HasAccess)
        {
            // Save succeeded
        }
        else
        {
            // Handle authorization failure: result.Message
        }
    }
}
```

## Getting Started

1. **[Installation](getting-started/installation.md)**: Install the NuGet packages and configure your projects
2. **[Quick Start](getting-started/quick-start.md)**: Build your first RemoteFactory application in 5 minutes
3. **[Project Structure](getting-started/project-structure.md)**: Recommended solution organization

## Core Concepts

- **[Architecture Overview](concepts/architecture-overview.md)**: Understand how RemoteFactory works
- **[Factory Operations](concepts/factory-operations.md)**: Create, Fetch, Insert, Update, Delete, and Execute
- **[Commands & Queries](concepts/factory-operations.md#commands--queries-pattern)**: Simple request-response patterns
- **[Three-Tier Execution](concepts/three-tier-execution.md)**: Server, Remote, and Logical modes
- **[Service Injection](concepts/service-injection.md)**: Using `[Service]` for dependency injection

## Authorization

- **[Authorization Overview](authorization/authorization-overview.md)**: Two approaches to access control
- **[Custom Authorization](authorization/custom-authorization.md)**: Using `[AuthorizeFactory<T>]`
- **[ASP.NET Core Integration](authorization/asp-authorize.md)**: Policy-based authorization
- **[Can Methods](authorization/can-methods.md)**: Generated authorization check methods

## Source Generation

- **[How It Works](source-generation/how-it-works.md)**: High-level understanding of code generation
- **[Factory Generator](source-generation/factory-generator.md)**: Factory generation details
- **[Troubleshooting](source-generation/how-it-works.md#troubleshooting)**: Common issues and solutions

## Reference

- **[Attributes](reference/attributes.md)**: Complete attribute reference
- **[Interfaces](reference/interfaces.md)**: All framework interfaces
- **[Factory Modes](reference/factory-modes.md)**: NeatooFactory enum reference
- **[Generated Code](reference/generated-code.md)**: Understanding generated factory structure

## Examples

- **[Blazor Application](examples/blazor-app.md)**: Complete Blazor WASM example
- **[WPF Application](examples/wpf-app.md)**: WPF with MVVM pattern
- **[Common Patterns](examples/common-patterns.md)**: Reusable patterns and recipes

## Framework Comparison

- **[Comparison Overview](comparison/overview.md)**: Framework comparison introduction
- **[vs CSLA](comparison/vs-csla.md)**: RemoteFactory compared to CSLA
- **[vs Manual DTOs](comparison/vs-dtos.md)**: RemoteFactory compared to manual DTO approach
- **[Decision Guide](comparison/decision-guide.md)**: When to use RemoteFactory

## Advanced Topics

- **[Factory Lifecycle](advanced/factory-lifecycle.md)**: IFactoryOnStart and IFactoryOnComplete hooks
- **[Interface Factories](advanced/interface-factories.md)**: Using `[Factory]` on interfaces
- **[Static Execute](advanced/static-execute.md)**: Static class Execute operations
- **[JSON Serialization](advanced/json-serialization.md)**: Custom serialization configuration
- **[Extending FactoryCore](advanced/extending-factory-core.md)**: Custom IFactoryCore implementations

## Resources

- [GitHub Repository](https://github.com/NeatooDotNet/RemoteFactory)
- [NuGet Package](https://www.nuget.org/packages/Neatoo.RemoteFactory)
- [Discord Community](https://discord.gg/M3dVuZkG)
