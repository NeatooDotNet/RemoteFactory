---
layout: default
title: "Three-Tier Execution"
description: "Understanding Server, Remote, and Logical execution modes in RemoteFactory"
parent: Concepts
nav_order: 3
---

# Three-Tier Execution

RemoteFactory supports three execution modes that determine how factory methods are invoked: Server, Remote, and Logical. Each mode serves a specific deployment scenario.

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

## Mode Comparison

| Aspect | Server | Remote | Logical |
|--------|--------|--------|---------|
| Deployment | ASP.NET Core server | Blazor WASM, WPF client | Unit tests, single-tier |
| [Remote] methods | Execute locally | Call server via HTTP | Execute locally (serialized) |
| Non-[Remote] methods | Execute locally | Execute locally | Execute locally |
| Services | Resolved from DI | Resolved from DI | Resolved from DI |
| Serialization | On response | On request/response | Full round-trip |

## Server Mode

Use Server mode in your ASP.NET Core application. This mode registers delegates that handle incoming remote calls.

### Configuration

```csharp
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// AddNeatooAspNetCore uses Server mode internally
builder.Services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);

var app = builder.Build();

// Maps the /api/neatoo endpoint
app.UseNeatoo();
```

### What Happens in Server Mode

1. **Factory Registration**: All generated factories are registered with DI
2. **Delegate Registration**: Each remote method gets a delegate registered
3. **Local Execution**: Factory methods execute directly on the server
4. **Service Resolution**: `[Service]` parameters resolve from `IServiceProvider`

### Request Handling Flow

```
Client HTTP POST to /api/neatoo
         │
         ▼
┌────────────────────────────┐
│ HandleRemoteDelegateRequest│
├────────────────────────────┤
│ 1. Deserialize request     │
│ 2. Resolve delegate by type│
│ 3. Invoke delegate         │
│ 4. Serialize response      │
└────────────────────────────┘
         │
         ▼
HTTP Response to Client
```

## Remote Mode

Use Remote mode in client applications like Blazor WebAssembly or WPF. Factory methods marked with `[Remote]` will call the server via HTTP.

### Configuration

```csharp
using Neatoo.RemoteFactory;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register factories in Remote mode
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);

// Configure HTTP client for remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://your-server.com/") };
});
```

### What Happens in Remote Mode

1. **Factory Registration**: Factories registered with `IMakeRemoteDelegateRequest`
2. **Remote Methods**: Serialize parameters, POST to server, deserialize result
3. **Local Methods**: Execute directly on client (Create usually)
4. **Service Resolution**: Only works for services registered on client

### Remote Method Flow

```csharp
// In your Blazor component
var person = await _factory.Fetch(123);
```

```
Factory.Fetch(123)
       │
       ▼
┌──────────────────────────┐
│ RemoteFetch delegate     │
├──────────────────────────┤
│ 1. Serialize: 123        │
│ 2. Create request DTO    │
│ 3. HTTP POST /api/neatoo │
│ 4. Deserialize response  │
│ 5. Return PersonModel    │
└──────────────────────────┘
       │
       ▼
PersonModel returned
```

### Handling Remote Errors

```csharp
try
{
    var person = await _factory.Fetch(123);
}
catch (HttpRequestException ex)
{
    // Network error - server unreachable
}
catch (NotAuthorizedException ex)
{
    // Authorization failed on server
    Console.WriteLine(ex.Authorized.Message);
}
```

## Logical Mode

Use Logical mode for unit testing or single-tier applications. Methods execute locally but objects are serialized/deserialized to test round-trip behavior.

### Configuration

```csharp
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register factories in Logical mode
services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);

// Register mock/test services
services.AddScoped<IPersonContext, InMemoryPersonContext>();

var provider = services.BuildServiceProvider();
```

### What Happens in Logical Mode

1. **Factory Registration**: Same as Remote mode structure
2. **No HTTP Calls**: Methods execute in-process
3. **Serialization**: Objects are serialized and deserialized (tests round-trip)
4. **Service Resolution**: Uses registered services (typically mocks)

### Why Use Logical Mode?

- **Test Serialization**: Verify objects serialize correctly
- **Integration Tests**: Test full factory operation without network
- **Single-Tier Apps**: Desktop apps that don't need a server

### Example: Unit Testing with Logical Mode

```csharp
public class PersonModelTests
{
    private IServiceProvider _provider;
    private IPersonModelFactory _factory;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddNeatooRemoteFactory(NeatooFactory.Logical,
            typeof(IPersonModel).Assembly);

        // Mock database
        services.AddScoped<IPersonContext, InMemoryPersonContext>();

        _provider = services.BuildServiceProvider();
        _factory = _provider.GetRequiredService<IPersonModelFactory>();
    }

    [Test]
    public async Task FetchPerson_ReturnsModel()
    {
        // Arrange - seed test data
        var context = _provider.GetRequiredService<IPersonContext>();
        context.Persons.Add(new PersonEntity { Id = 1, FirstName = "John" });

        // Act
        var person = await _factory.Fetch(1);

        // Assert
        Assert.NotNull(person);
        Assert.Equal("John", person.FirstName);
    }

    [Test]
    public async Task SavePerson_PersistsChanges()
    {
        // Arrange
        var person = _factory.Create();
        person.FirstName = "Jane";

        // Act
        var saved = await _factory.Save(person);

        // Assert
        var context = _provider.GetRequiredService<IPersonContext>();
        Assert.Single(context.Persons);
    }
}
```

## Method Execution by Mode

Understanding which methods run where is crucial:

### [Remote] Methods

```csharp
[Factory]
public class PersonModel
{
    [Remote]  // This method's execution depends on mode
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonContext context)
    {
        // Database access
    }
}
```

| Mode | Execution Location |
|------|-------------------|
| Server | Local (server process) |
| Remote | Server (via HTTP) |
| Logical | Local (serialized) |

### Non-[Remote] Methods

```csharp
[Factory]
public class PersonModel
{
    [Create]  // No [Remote] attribute
    public PersonModel()
    {
        // Simple construction
    }
}
```

| Mode | Execution Location |
|------|-------------------|
| Server | Local |
| Remote | Local (client) |
| Logical | Local |

## Constructor Selection

The generated factory has different constructors for each mode:

```csharp
internal class PersonModelFactory
{
    // Constructor for Server/Logical mode (no IMakeRemoteDelegateRequest)
    public PersonModelFactory(
        IServiceProvider serviceProvider,
        IFactoryCore<IPersonModel> factoryCore)
    {
        FetchProperty = LocalFetch;  // Execute locally
    }

    // Constructor for Remote mode (has IMakeRemoteDelegateRequest)
    public PersonModelFactory(
        IServiceProvider serviceProvider,
        IMakeRemoteDelegateRequest remoteMethodDelegate,
        IFactoryCore<IPersonModel> factoryCore)
    {
        FetchProperty = RemoteFetch;  // Call server
    }
}
```

The presence of `IMakeRemoteDelegateRequest` in DI determines which constructor is used.

## Mixing Modes

In a typical 3-tier application:

```
┌─────────────────────────────────────────┐
│              Blazor WASM                │
│         NeatooFactory.Remote            │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ IPersonModelFactory               │  │
│  │  - Create() → local              │  │
│  │  - Fetch() → HTTP to server      │  │
│  │  - Save() → HTTP to server       │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                    │
                    │ HTTP /api/neatoo
                    ▼
┌─────────────────────────────────────────┐
│            ASP.NET Core                 │
│         NeatooFactory.Server            │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ PersonModelFactory                │  │
│  │  - LocalFetch() → database       │  │
│  │  - LocalSave() → database        │  │
│  │  - Delegates registered          │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Best Practices

### 1. Keep Create Local

Create operations rarely need server resources:

```csharp
[Create]  // No [Remote] - runs on client
public PersonModel()
{
    Id = Guid.NewGuid();
    CreatedAt = DateTime.UtcNow;
}
```

### 2. Mark Database Operations as Remote

Any method accessing server-only resources:

```csharp
[Remote]  // Must run on server
[Fetch]
public async Task<bool> Fetch([Service] IDbContext context)
{
    // Database access
}
```

### 3. Use Logical Mode for Testing

Test business logic without network dependencies:

```csharp
[Test]
public void TestAuthorization()
{
    // Setup with Logical mode
    services.AddNeatooRemoteFactory(NeatooFactory.Logical, ...);

    // Test authorization without HTTP
    var canFetch = factory.CanFetch();
    Assert.True(canFetch.HasAccess);
}
```

## Next Steps

- **[Service Injection](service-injection.md)**: Using `[Service]` for DI
- **[Factory Modes Reference](../reference/factory-modes.md)**: Detailed enum documentation
- **[Architecture Overview](architecture-overview.md)**: Full architecture explanation
