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

<!-- pseudo:neatoo-factory-enum -->
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
    /// Logical factory for single-tier apps and unit tests - executes locally
    /// </summary>
    Logical
}
```

## Server Mode

Used in ASP.NET Core server applications.

### Configuration

<!-- snippet: docs:reference/factory-modes:server-configuration -->
```csharp
// Using the AspNetCore helper (recommended)
services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);

// Or using the base method directly
services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(IPersonModel).Assembly);
```
<!-- /snippet -->

### Behavior

| Aspect | Behavior |
|--------|----------|
| Factory methods | Execute locally |
| `[Remote]` methods | Execute locally |
| Service resolution | From server DI container |
| Delegate registration | Yes (for handling remote calls) |
| HTTP calls | None |

### What Gets Registered

<!-- pseudo:server-registrations -->
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

<!-- pseudo:server-constructor -->
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

<!-- snippet: docs:reference/factory-modes:remote-configuration -->
```csharp
services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);

// Must also configure HTTP client
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://your-server.com/") };
});
```
<!-- /snippet -->

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

<!-- pseudo:remote-registrations -->
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

<!-- pseudo:remote-constructor -->
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

<!-- snippet: docs:reference/factory-modes:http-client-configuration -->
```csharp
// Basic configuration
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
});

// With authentication
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    var tokenProvider = sp.GetRequiredService<ITokenProvider>();
    var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", tokenProvider.GetToken());
    return client;
});

// With custom headers
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
    client.DefaultRequestHeaders.Add("X-Api-Key", "your-api-key");
    client.DefaultRequestHeaders.Add("X-Client-Version", "1.0.0");
    return client;
});
```
<!-- /snippet -->

## Logical Mode

Used for single-tier applications and unit testing.

### Configuration

<!-- snippet: docs:reference/factory-modes:logical-configuration -->
```csharp
services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);
```
<!-- /snippet -->

### Behavior

| Aspect | Behavior |
|--------|----------|
| Factory methods | Execute locally |
| `[Remote]` methods | Execute locally |
| Service resolution | From DI container |
| Delegate registration | No |
| HTTP calls | None |
| Serialization | None |

Logical mode behaves identically to Server mode - both execute methods locally without serialization. The difference is semantic: Server mode implies an ASP.NET Core server, while Logical mode implies a single-tier application or test.

### What Gets Registered

<!-- pseudo:logical-registrations -->
```csharp
// Factory registrations
services.AddScoped<PersonModelFactory>();
services.AddScoped<IPersonModelFactory, PersonModelFactory>();

// Domain model types
services.AddTransient<PersonModel>();
services.AddTransient<IPersonModel, PersonModel>();

// No IMakeRemoteDelegateRequest - uses local constructor
```

### Constructor Selected

The factory constructor without `IMakeRemoteDelegateRequest` (same as Server mode):

<!-- pseudo:logical-constructor -->
```csharp
public PersonModelFactory(
    IServiceProvider serviceProvider,
    IFactoryCore<IPersonModel> factoryCore)
{
    // FetchProperty = LocalFetch (local execution)
}
```

### Use Cases

<!-- snippet: docs:reference/factory-modes:logical-use-cases -->
```csharp
// Unit testing
[Fact]
public void TestCreatePerson()
{
    var services = new ServiceCollection();
    services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);

    var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IPersonModelFactory>();

    var person = factory.Create();
    Assert.NotNull(person);
}

// Single-tier desktop app
public class DesktopApp
{
    public DesktopApp()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);
        // No HTTP, no server - everything local
    }
}
```
<!-- /snippet -->

## Mode Comparison Table

| Feature | Server | Remote | Logical |
|---------|--------|--------|---------|
| Use case | ASP.NET Core | Blazor/WPF | Testing/Single-tier |
| HTTP calls | Receives | Makes | None |
| Local execution | All methods | Non-[Remote] | All methods |
| Remote execution | N/A | [Remote] methods | N/A |
| Serialization | On response | Request/response | None |
| Delegate registration | Yes | No | No |
| Needs HttpClient | No | Yes | No |

## Switching Modes

A single codebase can run in different modes:

<!-- snippet: docs:reference/factory-modes:switching-modes -->
```csharp
// Determined by configuration
var mode = factoryModeSetting switch
{
    "Server" => NeatooFactory.Server,
    "Remote" => NeatooFactory.Remote,
    "Logical" => NeatooFactory.Logical,
    _ => NeatooFactory.Remote
};

services.AddNeatooRemoteFactory(mode, typeof(IPersonModel).Assembly);
```
<!-- /snippet -->

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

<!-- snippet: docs:reference/factory-modes:register-matching-name -->
```csharp
// Register types following naming convention
services.RegisterMatchingName(typeof(IPersonModel).Assembly);
```
<!-- /snippet -->

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

<!-- pseudo:manual-vs-automatic-registration -->
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
