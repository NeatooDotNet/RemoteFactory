---
layout: default
title: "Client-Server Setup"
description: "Step-by-step guide to setting up client-server code separation"
parent: Getting Started
nav_order: 4
---

# Client-Server Setup

This guide walks through setting up a project with client-server code separation, where the client assembly contains only remote factory stubs while the server assembly has full implementations.

## Prerequisites

- Familiarity with basic RemoteFactory setup (see [Quick Start](quick-start.md))
- Understanding of the pattern (see [Client-Server Separation](../concepts/client-server-separation.md))

## Step 1: Create Project Structure

Create the following project structure:

```
MyApp/
├── MyApp.Domain/                    # Shared source files (not a project)
│   ├── Order.cs
│   ├── IOrder.cs
│   └── OrderLine.cs
├── MyApp.Domain.Client/             # Client library project
│   ├── MyApp.Domain.Client.csproj
│   └── AssemblyAttributes.cs
├── MyApp.Domain.Server/             # Server library project
│   └── MyApp.Domain.Server.csproj
├── MyApp.Ef/                        # EF Core project
│   ├── MyApp.Ef.csproj
│   └── AppDbContext.cs
├── MyApp.Server/                    # ASP.NET Core API
│   └── Program.cs
└── MyApp.BlazorClient/              # Blazor WASM app
    └── Program.cs
```

Note: `MyApp.Domain/` is a folder, not a project. It contains shared source files that are linked into both Domain.Client and Domain.Server.

## Step 2: Configure Domain.Client Project

Create the client project that defines the `CLIENT` constant:

**MyApp.Domain.Client.csproj:**

<!-- pseudo:setup-client-csproj -->
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Define CLIENT constant for conditional compilation -->
    <DefineConstants>$(DefineConstants);CLIENT</DefineConstants>
    
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
  </ItemGroup>

  <!-- Link shared source files -->
  <ItemGroup>
    <Compile Include="..\MyApp.Domain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Neatoo.RemoteFactory" Version="10.8.0" />
    <!-- No EF reference here -->
  </ItemGroup>

</Project>
```

**AssemblyAttributes.cs:**

<!-- pseudo:setup-assembly-attributes -->
```csharp
using Neatoo.RemoteFactory;

// Generate remote-only factories for this assembly
[assembly: FactoryMode(FactoryMode.RemoteOnly)]
```

## Step 3: Configure Domain.Server Project

Create the server project that links the same source files:

**MyApp.Domain.Server.csproj:**

<!-- pseudo:setup-server-csproj -->
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- No CLIENT constant - full implementations -->
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
  </ItemGroup>

  <!-- Link same shared source files -->
  <ItemGroup>
    <Compile Include="..\MyApp.Domain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Neatoo.RemoteFactory" Version="10.8.0" />
    <!-- Server references EF project -->
    <ProjectReference Include="..\MyApp.Ef\MyApp.Ef.csproj" />
  </ItemGroup>

</Project>
```

## Step 4: Write Shared Domain Code

Create your entity with conditional compilation:

**MyApp.Domain/Order.cs:**

<!-- pseudo:setup-entity -->
```csharp
using Neatoo.RemoteFactory;
#if !CLIENT
using MyApp.Ef;
using Microsoft.EntityFrameworkCore;
#endif

namespace MyApp.Domain;

[Factory]
internal class Order : IOrder
{
    // Shared properties - compiled by both
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

#if CLIENT
    // Client placeholders
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Call through IOrderFactory");
    }

    [Remote, Fetch]
    public Task Fetch(Guid id)
    {
        throw new InvalidOperationException("Call through IOrderFactory");
    }

    [Remote, Insert]
    public Task Insert()
    {
        throw new InvalidOperationException("Call through IOrderFactory");
    }

    [Remote, Update]
    public Task Update()
    {
        throw new InvalidOperationException("Call through IOrderFactory");
    }
#else
    // Server implementations
    [Remote, Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    [Remote, Fetch]
    public async Task Fetch(Guid id, [Service] IAppDbContext db)
    {
        var entity = await db.Orders.FirstAsync(o => o.Id == id);
        Id = entity.Id;
        CustomerName = entity.CustomerName;
        IsNew = false;
    }

    [Remote, Insert]
    public async Task Insert([Service] IAppDbContext db)
    {
        db.Orders.Add(new OrderEntity { Id = Id, CustomerName = CustomerName });
        await db.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IAppDbContext db)
    {
        var entity = await db.Orders.FirstAsync(o => o.Id == Id);
        entity.CustomerName = CustomerName;
        await db.SaveChangesAsync();
    }
#endif
}
```

## Step 5: Configure Server Application

**MyApp.Server/Program.cs:**

<!-- pseudo:setup-server-program -->
```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using MyApp.Domain;
using MyApp.Ef;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

// Register factories from Domain.Server assembly
builder.Services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
builder.Services.AddScoped<IAppDbContext, AppDbContext>();
builder.Services.AddDbContext<AppDbContext>();

var app = builder.Build();

// Create database on startup (for development)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseNeatoo();
app.UseCors(b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

await app.RunAsync();
```

## Step 6: Configure Client Application

**MyApp.BlazorClient/Program.cs:**

<!-- pseudo:setup-client-program -->
```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;
using MyApp.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register RemoteOnly factories from Domain.Client
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IOrder).Assembly);

// Configure HTTP client for API calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("http://localhost:5184/") };
});

await builder.Build().RunAsync();
```

## Step 7: Verify Separation

After building, verify the client assembly has no EF types:

```bash
# Should return empty (no EF references)
strings MyApp.Domain.Client.dll | grep -i EntityFramework

# Compare with server assembly (should find EF types)
strings MyApp.Domain.Server.dll | grep -i EntityFramework
```

## Troubleshooting

### "Type not found" errors on client

Ensure the `CLIENT` constant is defined and the `#if CLIENT` sections don't reference server-only types.

### EF types appearing in client

Check that:
1. EF `using` statements are inside `#if !CLIENT`
2. Domain.Client.csproj doesn't reference the EF project
3. No transitive references bring in EF

### Generated factory missing methods

Verify:
1. Assembly attribute `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` is present
2. Methods have the `[Remote]` attribute
3. Build both projects and compare Generated/*.g.cs files

## Next Steps

- [Client-Server Separation Concepts](../concepts/client-server-separation.md) - Deeper understanding
- [Service Injection](../concepts/service-injection.md) - `[Service]` parameters
- [Authorization](../authorization/) - Securing factory operations
