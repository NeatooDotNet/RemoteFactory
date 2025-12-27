---
layout: default
title: "Installation"
description: "Install and configure Neatoo RemoteFactory for your .NET projects"
parent: Getting Started
nav_order: 1
---

# Installation

This guide covers installing the RemoteFactory NuGet packages and configuring your projects for code generation.

## Prerequisites

- **.NET 8.0 or later** (including .NET 9.0)
- **Visual Studio 2022**, **JetBrains Rider**, or **VS Code** with C# Dev Kit
- Basic familiarity with dependency injection in .NET

## NuGet Packages

RemoteFactory provides two NuGet packages:

| Package | Purpose | Install In |
|---------|---------|------------|
| `Neatoo.RemoteFactory` | Core library with attributes, interfaces, and source generators | Domain model projects, client projects |
| `Neatoo.RemoteFactory.AspNetCore` | ASP.NET Core integration with endpoint setup and authorization | Server projects |

### Install via Package Manager Console

```powershell
# For domain model and client projects
Install-Package Neatoo.RemoteFactory

# For ASP.NET Core server projects
Install-Package Neatoo.RemoteFactory.AspNetCore
```

### Install via .NET CLI

```bash
# For domain model and client projects
dotnet add package Neatoo.RemoteFactory

# For ASP.NET Core server projects
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

### Install via PackageReference

```xml
<!-- Domain model and client projects -->
<PackageReference Include="Neatoo.RemoteFactory" Version="9.*" />

<!-- ASP.NET Core server projects -->
<PackageReference Include="Neatoo.RemoteFactory.AspNetCore" Version="9.*" />
```

## Project Configuration

### Recommended Project Settings

Enable nullable reference types and implicit usings in your project files:

```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

### Source Generator Requirements

The RemoteFactory source generator works automatically when you install the NuGet package. No additional configuration is required.

Generated code appears in:
- **Visual Studio**: Dependencies > Analyzers > Neatoo.RemoteFactory.FactoryGenerator
- **File System**: `obj/Debug/net8.0/generated/Neatoo.RemoteFactory.FactoryGenerator/`

## Verify Installation

Create a simple class with the `[Factory]` attribute to verify code generation is working:

```csharp
using Neatoo.RemoteFactory;

namespace MyApp.DomainModel;

[Factory]
public class TestModel
{
    [Create]
    public TestModel() { }
}
```

After building, you should see:
1. A generated `ITestModelFactory` interface
2. A generated `TestModelFactory` class
3. No compilation errors

Check that IntelliSense recognizes `ITestModelFactory`:

```csharp
// This should compile and show IntelliSense
ITestModelFactory factory = null!;
var model = factory.Create();
```

## Solution Structure

A typical RemoteFactory solution has three projects:

```
MySolution/
├── MyApp.DomainModel/          # Shared domain models
│   ├── MyApp.DomainModel.csproj
│   └── PersonModel.cs
├── MyApp.Server/               # ASP.NET Core server
│   ├── MyApp.Server.csproj
│   └── Program.cs
└── MyApp.Client/               # Blazor WASM or WPF client
    ├── MyApp.Client.csproj
    └── Program.cs
```

### Project References

```
MyApp.Server    -->  MyApp.DomainModel
MyApp.Client    -->  MyApp.DomainModel
```

### Package References

| Project | Packages |
|---------|----------|
| MyApp.DomainModel | `Neatoo.RemoteFactory` |
| MyApp.Server | `Neatoo.RemoteFactory.AspNetCore` |
| MyApp.Client | `Neatoo.RemoteFactory` |

## Server Configuration

In your ASP.NET Core server's `Program.cs`:

```csharp
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using MyApp.DomainModel;

var builder = WebApplication.CreateBuilder(args);

// Add RemoteFactory services in Server mode
// Include the assembly containing your domain models
builder.Services.AddNeatooAspNetCore(typeof(PersonModel).Assembly);

// Register your application services
builder.Services.AddScoped<IPersonContext, PersonContext>();

var app = builder.Build();

// Add the RemoteFactory endpoint
app.UseNeatoo();

app.Run();
```

The `AddNeatooAspNetCore` method:
- Registers all generated factories
- Configures JSON serialization for domain objects
- Registers the delegate handlers for remote calls
- Enables ASP.NET Core authorization integration

The `UseNeatoo` method:
- Maps the `/api/neatoo` POST endpoint for remote factory calls

## Client Configuration

### Blazor WebAssembly

```csharp
using Neatoo.RemoteFactory;
using MyApp.DomainModel;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add RemoteFactory services in Remote mode
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(PersonModel).Assembly);

// Configure the HTTP client for remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };
});

await builder.Build().RunAsync();
```

### WPF or Console Applications

```csharp
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using MyApp.DomainModel;

var services = new ServiceCollection();

// Add RemoteFactory services in Remote mode
services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(PersonModel).Assembly);

// Configure the HTTP client for remote calls
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };
});

var serviceProvider = services.BuildServiceProvider();
```

### Unit Testing (Logical Mode)

For unit tests where you want local execution without HTTP:

```csharp
using Neatoo.RemoteFactory;
using MyApp.DomainModel;

var services = new ServiceCollection();

// Use Logical mode for in-process testing
services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(PersonModel).Assembly);

// Register mock services
services.AddScoped<IPersonContext, MockPersonContext>();

var serviceProvider = services.BuildServiceProvider();
```

## Multiple Assembly Registration

If your domain models span multiple assemblies:

```csharp
builder.Services.AddNeatooAspNetCore(
    typeof(PersonModel).Assembly,
    typeof(OrderModel).Assembly,
    typeof(InventoryModel).Assembly
);
```

## Troubleshooting

### Generated Code Not Appearing

1. **Rebuild the solution**: Sometimes a clean rebuild is needed
2. **Check for errors**: Look in the Error List for source generator diagnostics
3. **Verify package installation**: Ensure `Neatoo.RemoteFactory` is installed correctly

### IntelliSense Not Working

1. **Restart Visual Studio or Rider**: IDE caches may be stale
2. **Clear NuGet caches**: `dotnet nuget locals all --clear`
3. **Check SDK version**: Ensure you're using .NET 8.0 SDK or later

### Build Errors with Generated Code

1. **Class must not be abstract**: The `[Factory]` attribute requires a concrete class
2. **Class must not be generic**: Generic classes are not supported
3. **Partial classes for mappers**: If using `MapTo`/`MapFrom`, the class must be `partial`

## Next Steps

- **[Quick Start](quick-start.md)**: Build your first RemoteFactory application
- **[Project Structure](project-structure.md)**: Recommended solution organization
- **[Architecture Overview](../concepts/architecture-overview.md)**: Understand how RemoteFactory works
