---
layout: default
title: "Factory Modes"
description: "NeatooFactory enum reference - Server, Remote, and Logical modes"
parent: Reference
nav_order: 3
---

# Factory Modes Reference

The `NeatooFactory` enum determines how factories execute operations. This document provides a complete reference for each mode.

## NeatooFactory Enum

```csharp
public enum NeatooFactory
{
    /// <summary>
    /// Server in a 3-tier architecture - executes delegates locally
    /// </summary>
    Server,

    /// <summary>
    /// Client in a 3-tier architecture - calls server via HTTP
    /// </summary>
    Remote,

    /// <summary>
    /// Logical factory - local execution with serialization (for testing)
    /// </summary>
    Logical
}
```

## Server Mode

Used in ASP.NET Core server applications.

### Configuration

```csharp
// Using the AspNetCore helper (recommended)
builder.Services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);

// Or using the base method directly
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(IPersonModel).Assembly);
```

### Behavior

| Aspect | Behavior |
|--------|----------|
| Factory methods | Execute locally |
| `[Remote]` methods | Execute locally |
| Service resolution | From server DI container |
| Delegate registration | Yes (for handling remote calls) |
| HTTP calls | None |

### What Gets Registered

```csharp
// Factories
services.AddScoped<PersonModelFactory>();
services.AddScoped<IPersonModelFactory, PersonModelFactory>();

// Delegates (for remote call handling)
services.AddScoped<FetchDelegate>(cc => ...);
services.AddScoped<SaveDelegate>(cc => ...);

// Domain model types
services.AddTransient<PersonModel>();
services.AddTransient<IPersonModel, PersonModel>();

// Remote request handler
services.AddTransient<HandleRemoteDelegateRequest>(s => LocalServer.HandlePortalRequest(s));
```

### Constructor Selected

The factory constructor without `IMakeRemoteDelegateRequest`:

```csharp
public PersonModelFactory(
    IServiceProvider serviceProvider,
    IFactoryCore<IPersonModel> factoryCore)
{
    // FetchProperty = LocalFetch (local execution)
}
```

## Remote Mode

Used in client applications (Blazor WASM, WPF, etc.).

### Configuration

```csharp
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);

// Must also configure HTTP client
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://your-server.com/") };
});
```

### Behavior

| Aspect | Behavior |
|--------|----------|
| Factory methods | Depends on method |
| `[Remote]` methods | Call server via HTTP |
| Non-`[Remote]` methods | Execute locally |
| Service resolution | From client DI container |
| Delegate registration | No |
| HTTP calls | Yes (to `/api/neatoo`) |

### What Gets Registered

```csharp
// Factories
services.AddScoped<PersonModelFactory>();
services.AddScoped<IPersonModelFactory, PersonModelFactory>();

// Remote delegate request maker
services.AddScoped<IMakeRemoteDelegateRequest, MakeRemoteDelegateRequest>();

// Domain model types
services.AddTransient<PersonModel>();
services.AddTransient<IPersonModel, PersonModel>();

// HTTP call implementation
services.AddTransient(sp => {
    var httpClient = sp.GetRequiredKeyedService<HttpClient>(HttpClientKey);
    return MakeRemoteDelegateRequestHttpCallImplementation.Create(httpClient);
});
```

### Constructor Selected

The factory constructor with `IMakeRemoteDelegateRequest`:

```csharp
public PersonModelFactory(
    IServiceProvider serviceProvider,
    IMakeRemoteDelegateRequest remoteMethodDelegate,
    IFactoryCore<IPersonModel> factoryCore)
{
    // FetchProperty = RemoteFetch (HTTP call)
}
```

### HTTP Client Configuration

```csharp
// Basic configuration
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
});

// With authentication
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    var tokenProvider = sp.GetRequiredService<ITokenProvider>();
    var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", tokenProvider.GetToken());
    return client;
});

// With custom headers
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
    client.DefaultRequestHeaders.Add("X-Api-Key", "your-api-key");
    client.DefaultRequestHeaders.Add("X-Client-Version", "1.0.0");
    return client;
});
```

## Logical Mode

Used for testing or single-tier applications.

### Configuration

```csharp
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);
```

### Behavior

| Aspect | Behavior |
|--------|----------|
| Factory methods | Execute locally |
| `[Remote]` methods | Execute locally (with serialization) |
| Service resolution | From DI container |
| Delegate registration | No |
| HTTP calls | None |
| Serialization | Yes (simulates network) |

### What Gets Registered

```csharp
// Same factory registrations as Remote mode
services.AddScoped<PersonModelFactory>();
services.AddScoped<IPersonModelFactory, PersonModelFactory>();

// Local serialized delegate request (no HTTP)
services.AddScoped<IMakeRemoteDelegateRequest, MakeLocalSerializedDelegateRequest>();

// Domain model types
services.AddTransient<PersonModel>();
services.AddTransient<IPersonModel, PersonModel>();
```

### Why Serialization?

Logical mode serializes and deserializes objects even though no HTTP call is made. This:

1. **Tests serialization**: Ensures objects serialize correctly
2. **Simulates network**: Catches issues that would appear in remote mode
3. **Validates types**: Ensures all types are serialization-compatible

### Use Cases

```csharp
// Unit testing
[Test]
public async Task TestFetchPerson()
{
    var services = new ServiceCollection();
    services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);
    services.AddScoped<IPersonContext, InMemoryPersonContext>();

    var provider = services.BuildServiceProvider();
    var factory = provider.GetRequiredService<IPersonModelFactory>();

    var person = await factory.Fetch(123);
    Assert.NotNull(person);
}

// Single-tier desktop app
public class DesktopApp
{
    public DesktopApp()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);
        services.AddDbContext<AppDbContext>();
        // No HTTP, no server - everything local
    }
}
```

## Mode Comparison Table

| Feature | Server | Remote | Logical |
|---------|--------|--------|---------|
| Use case | ASP.NET Core | Blazor/WPF | Testing |
| HTTP calls | Receives | Makes | None |
| Local execution | All methods | Non-[Remote] | All methods |
| Remote execution | N/A | [Remote] methods | Simulated |
| Serialization | On response | Request/response | Full round-trip |
| Delegate registration | Yes | No | No |
| Needs HttpClient | No | Yes | No |

## Switching Modes

A single codebase can run in different modes:

```csharp
// Determined by configuration
var mode = configuration["FactoryMode"] switch
{
    "Server" => NeatooFactory.Server,
    "Remote" => NeatooFactory.Remote,
    "Logical" => NeatooFactory.Logical,
    _ => NeatooFactory.Remote
};

services.AddNeatooRemoteFactory(mode, typeof(IPersonModel).Assembly);
```

## Best Practices

### Use Server Mode For

- ASP.NET Core applications
- Server-side Blazor (if desired)
- Console apps acting as servers

### Use Remote Mode For

- Blazor WebAssembly
- WPF applications
- Console apps calling a server
- Any .NET client calling a remote server

### Use Logical Mode For

- Unit tests
- Integration tests
- Single-tier desktop apps
- Prototype/demo applications

## Helper Methods

### RegisterMatchingName

Auto-registers interface-to-implementation pairs where names follow the `I{Name}` convention:

```csharp
// Register types following naming convention
services.RegisterMatchingName(typeof(IPersonModel).Assembly);
```

**Matching rules:**
- Interface name must start with `I`
- Implementation must have same name without `I` prefix
- Both must be in the same assembly
- Implementation must be a non-abstract class

**Example registrations:**

| Interface | Implementation | Registered? |
|-----------|----------------|-------------|
| `IPersonService` | `PersonService` | Yes |
| `IOrderRepository` | `OrderRepository` | Yes |
| `ILogger` | `FileLogger` | No (names don't match) |
| `IAbstractBase` | `AbstractBase` (abstract) | No |

```csharp
// Before (manual registration)
services.AddTransient<IPersonService, PersonService>();
services.AddTransient<IOrderService, OrderService>();
services.AddTransient<ICustomerService, CustomerService>();
// ... many more

// After (automatic registration)
services.RegisterMatchingName(typeof(IPersonService).Assembly);
```

**Note:** This is a convenience method for domain services. Factory interfaces and implementations are automatically registered by `AddNeatooRemoteFactory`.

## Next Steps

- **[Three-Tier Execution](../concepts/three-tier-execution.md)**: Detailed execution flow
- **[Architecture Overview](../concepts/architecture-overview.md)**: Full system architecture
- **[Installation](../getting-started/installation.md)**: Setup instructions
