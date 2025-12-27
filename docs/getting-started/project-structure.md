---
layout: default
title: "Project Structure"
description: "Recommended project organization for RemoteFactory applications"
parent: Getting Started
nav_order: 3
---

# Project Structure

This guide describes the recommended project organization for RemoteFactory applications, covering solution structure, assembly configuration, and dependency management.

## Recommended Solution Structure

A typical RemoteFactory solution has three projects:

```
MySolution/
├── MySolution.sln
├── MyApp.DomainModel/              # Shared domain models
│   ├── MyApp.DomainModel.csproj
│   ├── Models/
│   │   ├── PersonModel.cs
│   │   ├── OrderModel.cs
│   │   └── ...
│   ├── Authorization/
│   │   ├── IPersonModelAuth.cs
│   │   └── PersonModelAuth.cs
│   └── Generated/                  # Generated code (in obj folder)
│
├── MyApp.Server/                   # ASP.NET Core server
│   ├── MyApp.Server.csproj
│   └── Program.cs
│
├── MyApp.Client/                   # Blazor WASM or WPF client
│   ├── MyApp.Client.csproj
│   ├── Program.cs
│   └── Pages/
│       └── ...
│
└── MyApp.Ef/                       # Entity Framework Core (optional)
    ├── MyApp.Ef.csproj
    ├── AppDbContext.cs
    ├── Entities/
    │   ├── PersonEntity.cs
    │   └── OrderEntity.cs
    └── Migrations/
```

## Project Dependencies

```
                    ┌─────────────────┐
                    │ MyApp.Server    │
                    │ (ASP.NET Core)  │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
    ┌─────────────────┐ ┌────────────┐ ┌──────────────┐
    │ MyApp.DomainModel│ │ MyApp.Ef   │ │ MyApp.Client │
    │   (Shared)       │ │ (EF Core)  │ │ (Blazor)     │
    └─────────────────┘ └────────────┘ └──────────────┘
              ▲              │              │
              │              │              │
              └──────────────┴──────────────┘
```

## Project Files

### MyApp.DomainModel.csproj

The shared domain model project:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- RemoteFactory with source generators -->
    <PackageReference Include="Neatoo.RemoteFactory" Version="9.*" />

    <!-- Optional: EF Core for entity types (if not in separate project) -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.*" />
  </ItemGroup>

</Project>
```

### MyApp.Server.csproj

The ASP.NET Core server project:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- RemoteFactory ASP.NET Core integration -->
    <PackageReference Include="Neatoo.RemoteFactory.AspNetCore" Version="9.*" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference domain model and EF projects -->
    <ProjectReference Include="..\MyApp.DomainModel\MyApp.DomainModel.csproj" />
    <ProjectReference Include="..\MyApp.Ef\MyApp.Ef.csproj" />
  </ItemGroup>

</Project>
```

### MyApp.Client.csproj

The Blazor WebAssembly client project:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Standard Blazor packages -->
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.*" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="8.*" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference domain model only (not server or EF) -->
    <ProjectReference Include="..\MyApp.DomainModel\MyApp.DomainModel.csproj" />
  </ItemGroup>

</Project>
```

### MyApp.Ef.csproj

The Entity Framework Core project (optional but recommended):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
    <!-- Or SQLite, PostgreSQL, etc. -->
  </ItemGroup>

  <ItemGroup>
    <!-- Reference domain model for entity types -->
    <ProjectReference Include="..\MyApp.DomainModel\MyApp.DomainModel.csproj" />
  </ItemGroup>

</Project>
```

## Assembly Registration

RemoteFactory needs to know which assemblies contain domain models. Register them in both server and client:

### Server Registration

```csharp
// Program.cs
using Neatoo.RemoteFactory.AspNetCore;
using MyApp.DomainModel;

var builder = WebApplication.CreateBuilder(args);

// Register domain model assembly
builder.Services.AddNeatooAspNetCore(typeof(PersonModel).Assembly);

// If models are in multiple assemblies
builder.Services.AddNeatooAspNetCore(
    typeof(PersonModel).Assembly,      // MyApp.DomainModel
    typeof(OrderModel).Assembly        // MyApp.OrderModule
);

var app = builder.Build();
app.UseNeatoo();
app.Run();
```

### Client Registration

```csharp
// Program.cs
using Neatoo.RemoteFactory;
using MyApp.DomainModel;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register domain model assembly in Remote mode
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    typeof(PersonModel).Assembly
);

// Configure HTTP client
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };
});

await builder.Build().RunAsync();
```

## File Organization

### Domain Model Project

```
MyApp.DomainModel/
├── Models/
│   ├── PersonModel.cs          # [Factory] domain model
│   ├── IPersonModel.cs         # Interface (optional but recommended)
│   ├── OrderModel.cs
│   └── IOrderModel.cs
├── Authorization/
│   ├── IPersonModelAuth.cs     # Authorization interface
│   └── PersonModelAuth.cs      # Authorization implementation
├── Services/
│   └── ICurrentUser.cs         # Shared service interfaces
└── Generated/
    └── ... (generated at build time)
```

### Server Project

```
MyApp.Server/
├── Program.cs                  # Entry point with DI configuration
├── Services/
│   ├── CurrentUser.cs          # ICurrentUser implementation
│   └── ...                     # Other server services
├── appsettings.json
└── appsettings.Development.json
```

### Client Project

```
MyApp.Client/
├── Program.cs                  # Entry point with DI configuration
├── Pages/
│   ├── Index.razor
│   ├── PersonEditor.razor
│   └── ...
├── Shared/
│   ├── MainLayout.razor
│   └── NavMenu.razor
├── Services/
│   └── ClientUser.cs           # Client-side ICurrentUser
└── wwwroot/
    └── ...
```

## Separation of Concerns

### What Goes Where

| Item | Project |
|------|---------|
| Domain models (`[Factory]`) | DomainModel |
| Domain interfaces | DomainModel |
| Authorization interfaces | DomainModel |
| Authorization implementations | DomainModel or Server |
| Entity classes | Ef (or DomainModel) |
| DbContext | Ef |
| Server services | Server |
| Blazor components | Client |
| Client services | Client |

### Keep Client Light

The client project should only reference:
- DomainModel (for types and generated factories)
- UI frameworks (Blazor, etc.)

The client should NOT reference:
- Ef project
- Server project
- Database packages

### Server Has Everything

The server project references all other projects and can:
- Access the database
- Run all factory operations locally
- Resolve server-only services

## Shared Service Interfaces

For services needed on both client and server:

```csharp
// In DomainModel/Services/ICurrentUser.cs
public interface ICurrentUser
{
    string UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    bool HasRole(string role);
}

// In Server/Services/ServerCurrentUser.cs
public class ServerCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;

    public string UserId =>
        _httpContext.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    // ... other properties
}

// In Client/Services/ClientCurrentUser.cs
public class ClientCurrentUser : ICurrentUser
{
    private readonly AuthenticationStateProvider _authProvider;

    public string UserId { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var state = await _authProvider.GetAuthenticationStateAsync();
        UserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }
}
```

## Best Practices

### 1. Use Interfaces for Domain Models

```csharp
// Allows better testing and abstraction
public interface IPersonModel : IFactorySaveMeta
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
}

[Factory]
public class PersonModel : IPersonModel { }
```

### 2. Keep Authorization With Domain

```csharp
// Authorization interface in DomainModel
public interface IPersonModelAuth
{
    [AuthorizeFactory(...)]
    bool CanCreate();
}

// Implementation in DomainModel (if it has no server dependencies)
// OR in Server (if it needs DbContext, etc.)
```

### 3. Separate Entities From Domain

```csharp
// In Ef project
public class PersonEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    // Database-specific concerns
}

// In DomainModel project
[Factory]
public class PersonModel : IPersonModel
{
    // Business-focused concerns
    public partial void MapFrom(PersonEntity entity);
}
```

### 4. Use RegisterMatchingName for Auto-Registration

```csharp
// Automatically registers IFoo to Foo
builder.Services.RegisterMatchingName(typeof(IPersonModelAuth).Assembly);

// Instead of manually:
// builder.Services.AddScoped<IPersonModelAuth, PersonModelAuth>();
```

## Next Steps

- **[Quick Start](quick-start.md)**: Build your first feature
- **[Architecture Overview](../concepts/architecture-overview.md)**: Understand the data flow
- **[Examples](../examples/blazor-app.md)**: See complete implementations
