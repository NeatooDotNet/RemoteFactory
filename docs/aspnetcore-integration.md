---
title: ASP.NET Core Integration
nav_order: 12
---

# ASP.NET Core Integration

RemoteFactory.AspNetCore provides server-side integration for ASP.NET Core applications. It registers the `/api/neatoo` endpoint, handles remote delegate requests, and manages serialization, authorization, and lifecycle hooks.

## Installation

Install the ASP.NET Core integration package:

```bash
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

This package depends on `Neatoo.RemoteFactory` and includes all core functionality.

## Basic Setup

Configure services and middleware in `Program.cs`:

<!-- snippet: aspnetcore-basic-setup -->
<a id='snippet-aspnetcore-basic-setup'></a>
```cs
public static class BasicSetup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Add Neatoo ASP.NET Core services
        services.AddNeatooAspNetCore(typeof(AspNetCoreIntegrationSamples).Assembly);
    }

    public static void ConfigureApp(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        // Map Neatoo endpoint at /api/neatoo
        app.UseNeatoo();
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L15-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-basic-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The setup follows this pattern:

1. **AddNeatooAspNetCore** - Registers factories, serialization, and authorization services
2. **UseNeatoo** - Adds the `/api/neatoo` endpoint middleware

## AddNeatooAspNetCore

Registers RemoteFactory services with default serialization options (Ordinal format).

```csharp
public static IServiceCollection AddNeatooAspNetCore(
    this IServiceCollection services,
    params Assembly[] entityLibraries)
```

**Parameters:**
- `entityLibraries` - Assemblies containing domain models with `[Factory]` attributes

**What it registers:**
- Generated factories (scoped)
- Serialization services (`NeatooJsonSerializer`, `NeatooSerializationOptions`)
- Authorization (`IAspAuthorize`, default implementation)
- Remote delegate handler (`HandleRemoteDelegateRequest`)
- Event tracker hosted service for graceful shutdown

Basic registration with domain model assembly:

<!-- snippet: aspnetcore-addneatoo -->
<a id='snippet-aspnetcore-addneatoo'></a>
```cs
public static class AddNeatooExample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register with single domain assembly
        services.AddNeatooAspNetCore(typeof(AspNetCoreIntegrationSamples).Assembly);

        // Or register with multiple assemblies
        // services.AddNeatooAspNetCore(
        //     typeof(Domain.Person).Assembly,
        //     typeof(Domain.Order).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L32-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-addneatoo' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Custom Serialization Options

Override default serialization with custom options:

```csharp
public static IServiceCollection AddNeatooAspNetCore(
    this IServiceCollection services,
    NeatooSerializationOptions serializationOptions,
    params Assembly[] entityLibraries)
```

Use Named format for debugging:

<!-- snippet: aspnetcore-custom-serialization -->
<a id='snippet-aspnetcore-custom-serialization'></a>
```cs
public static class CustomSerializationSetup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var serializationOptions = new NeatooSerializationOptions
        {
            // Named format: traditional JSON objects (larger but readable)
            Format = SerializationFormat.Named
        };

        services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(AspNetCoreIntegrationSamples).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L48-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-custom-serialization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Serialization](serialization.md) for format details.

## UseNeatoo

Configures the `/api/neatoo` endpoint for remote delegate requests.

```csharp
public static WebApplication UseNeatoo(this WebApplication app)
```

**What it does:**
- Maps POST endpoint at `/api/neatoo`
- Adds correlation ID middleware
- Extracts `X-Correlation-Id` from request headers
- Includes `X-Neatoo-Format` header in response
- Supports cancellation via client disconnect and server shutdown
- Configures ambient logging with application's logger factory

Middleware order matters:

<!-- snippet: aspnetcore-middleware-order -->
<a id='snippet-aspnetcore-middleware-order'></a>
```cs
public static class MiddlewareOrderExample
{
    public static void ConfigureApp(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        // Middleware order matters
        // 1. CORS must come before UseNeatoo if client is cross-origin
        app.UseCors();

        // 2. Authentication/Authorization if using [AspAuthorize]
        app.UseAuthentication();
        app.UseAuthorization();

        // 3. Neatoo endpoint
        app.UseNeatoo();

        // 4. Other endpoints
        // app.MapControllers();
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L66-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-middleware-order' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Endpoint Details

### Request Format

The client sends a `POST` to `/api/neatoo` with this payload:

```csharp
public class RemoteRequestDto
{
    public string Target { get; set; }      // Factory operation identifier
    public string? Args { get; set; }       // JSON-serialized arguments
}
```

**You do not create this manually.** Generated factories construct it automatically.

### Response Format

The server returns a `RemoteResponse`:

```csharp
public class RemoteResponse
{
    public object? Result { get; set; }
    public bool Authorized { get; set; }
    public string? Error { get; set; }
}
```

**Headers:**
- `X-Neatoo-Format`: `ordinal` or `named` (indicates serialization format)
- `X-Correlation-Id`: Correlation ID for distributed tracing

### Cancellation Support

The endpoint supports cancellation from multiple sources:

- **Client disconnect** - Detects when the HTTP request is aborted
- **Server shutdown** - Responds to `IHostApplicationLifetime.ApplicationStopping`
- **Explicit cancellation** - Honors `CancellationToken` passed to factory methods

Cancellation flow:

<!-- snippet: aspnetcore-cancellation -->
<a id='snippet-aspnetcore-cancellation'></a>
```cs
[Factory]
public partial class CancellationSupportedEntity
{
    public Guid Id { get; private set; }
    public bool Completed { get; private set; }

    [Create]
    public CancellationSupportedEntity() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IPersonRepository repository,
        CancellationToken cancellationToken)
    {
        // CancellationToken receives signal from:
        // 1. Client disconnect (HttpContext.RequestAborted)
        // 2. Server shutdown (IHostApplicationLifetime.ApplicationStopping)

        cancellationToken.ThrowIfCancellationRequested();

        // Pass to async operations
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        Id = id;
        Completed = true;
        return entity != null;
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L88-L118' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Correlation IDs

RemoteFactory automatically manages correlation IDs for distributed tracing.

**Client-side:**
- Generates a unique correlation ID for each request
- Includes it in the `X-Correlation-Id` header

**Server-side:**
- Extracts correlation ID from request headers
- Makes it available via `CorrelationContext.CorrelationId`
- Includes it in response headers
- Adds it to structured logs

Access the correlation ID in factory methods:

<!-- snippet: aspnetcore-correlation-id -->
<a id='snippet-aspnetcore-correlation-id'></a>
```cs
[Factory]
public partial class CorrelationIdExample
{
    public Guid Id { get; private set; }
    public string? CorrelationId { get; private set; }

    [Create]
    public CorrelationIdExample() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id, [Service] IAuditLogService auditLog)
    {
        // CorrelationContext.CorrelationId is automatically populated from
        // X-Correlation-Id header (or generated if not present)
        CorrelationId = CorrelationContext.CorrelationId;

        // Use for distributed tracing
        _ = auditLog.LogAsync("Fetch", id, "Entity", $"Correlation: {CorrelationId}", default);

        Id = id;
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L120-L144' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Logging

RemoteFactory integrates with `ILoggerFactory` for structured logging.

**Registered loggers:**
- `Neatoo.RemoteFactory.Server` - Server-side request handling
- `Neatoo.RemoteFactory.Client` - Client-side HTTP calls
- `Neatoo.RemoteFactory.Serialization` - Serialization diagnostics

Configure log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Neatoo.RemoteFactory": "Information"
    }
  }
}
```

**Log categories:**
- `NeatooLoggerCategories.Server` - Remote delegate execution
- `NeatooLoggerCategories.Client` - HTTP client requests
- `NeatooLoggerCategories.Serialization` - JSON serialization/deserialization

Log structured data in factory methods:

<!-- snippet: aspnetcore-logging -->
<a id='snippet-aspnetcore-logging'></a>
```cs
public static class LoggingConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);

            // Neatoo-specific log categories:
            // - Neatoo.RemoteFactory.Server - server-side request handling
            // - Neatoo.RemoteFactory.Client - client-side HTTP calls
            // - Neatoo.RemoteFactory.Serialization - serialization details

            builder.AddFilter("Neatoo.RemoteFactory", Microsoft.Extensions.Logging.LogLevel.Debug);
        });

        services.AddNeatooAspNetCore(typeof(AspNetCoreIntegrationSamples).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L146-L167' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-logging' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Event Tracking

RemoteFactory uses `IEventTracker` for fire-and-forget domain events.

**EventTrackerHostedService** is registered automatically by `AddNeatooAspNetCore()`. It:
- Tracks pending event handlers across all scopes
- Waits for completion during graceful shutdown
- Logs warnings for events that exceed shutdown timeout

**Shutdown behavior:**
- `IHostApplicationLifetime.ApplicationStopping` triggers shutdown
- Event handlers receive cancellation signal via `CancellationToken`
- Server waits up to 30 seconds for event completion
- Logs incomplete events after timeout

See [Events](events.md) for details on domain events.

## Authorization

ASP.NET Core integration includes authorization support via `IAspAuthorize`.

**Default implementation (`AspAuthorize`):**
- Registered automatically by `AddNeatooAspNetCore()`
- Integrates with ASP.NET Core's authorization system
- Uses `[AspAuthorize]` attributes on factory methods
- Supports policies, roles, and authentication schemes

**Interface:**
```csharp
public interface IAspAuthorize
{
    Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false);
}
```

The `AspAuthorize` implementation coordinates with ASP.NET Core's `IAuthorizationPolicyProvider` and `IPolicyEvaluator` to evaluate authorization requirements. Custom implementations can override this behavior by registering a different `IAspAuthorize` implementation.

See [Authorization](authorization.md) for complete authorization patterns, including custom implementations.

## Service Registration

Register your domain services alongside RemoteFactory:

<!-- snippet: aspnetcore-service-registration -->
<a id='snippet-aspnetcore-service-registration'></a>
```cs
public static class ServiceRegistration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register Neatoo
        services.AddNeatooAspNetCore(typeof(AspNetCoreIntegrationSamples).Assembly);

        // Register domain services (available in [Service] parameters)
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserContext, MockUserContext>();
        services.AddScoped<IEmailService, MockEmailService>();
        services.AddScoped<IAuditLogService, MockAuditLogService>();

        // Auto-register matching interfaces/implementations
        services.RegisterMatchingName(typeof(AspNetCoreIntegrationSamples).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L209-L228' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-service-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**RegisterMatchingName** is a convenience method that registers interfaces and their implementations as transient when they follow the `IName` → `Name` pattern.

Services are injected into factory methods via `[Service]` parameters.

## Multi-Assembly Support

Register factories from multiple assemblies:

<!-- snippet: aspnetcore-multi-assembly -->
<a id='snippet-aspnetcore-multi-assembly'></a>
```cs
public static class MultiAssemblySetup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register multiple domain assemblies
        services.AddNeatooAspNetCore(
            typeof(AspNetCoreIntegrationSamples).Assembly
            // typeof(OtherDomain.Entity).Assembly,
            // typeof(AnotherDomain.Model).Assembly
        );

        // Register matching services from all assemblies
        services.RegisterMatchingName(
            typeof(AspNetCoreIntegrationSamples).Assembly
            // typeof(OtherDomain.Entity).Assembly
        );
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L230-L249' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-multi-assembly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each assembly can contain domain models with `[Factory]` attributes. The generator processes all of them.

## Development vs Production

### Development

Use Named serialization format for easier debugging:

<!-- snippet: aspnetcore-development -->
<a id='snippet-aspnetcore-development'></a>
```cs
public static class DevelopmentConfiguration
{
    public static void ConfigureServices(IServiceCollection services, bool isDevelopment)
    {
        // Use readable JSON format in development
        var options = new NeatooSerializationOptions
        {
            Format = isDevelopment
                ? SerializationFormat.Named  // Readable JSON
                : SerializationFormat.Ordinal // Compact arrays
        };

        services.AddNeatooAspNetCore(options, typeof(AspNetCoreIntegrationSamples).Assembly);

        if (isDevelopment)
        {
            // Enable detailed logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddFilter("Neatoo", Microsoft.Extensions.Logging.LogLevel.Trace);
            });
        }
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L251-L277' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-development' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Production

Use Ordinal format (default) for compact payloads:

<!-- snippet: aspnetcore-production -->
<a id='snippet-aspnetcore-production'></a>
```cs
public static class ProductionConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Ordinal format for minimal payload size (default)
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };

        services.AddNeatooAspNetCore(options, typeof(AspNetCoreIntegrationSamples).Assembly);

        // Production logging - less verbose
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
            builder.AddFilter("Neatoo", Microsoft.Extensions.Logging.LogLevel.Information);
        });
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L279-L300' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-production' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Ordinal format reduces payload size by 40-50% compared to Named format.

## Error Handling

The `/api/neatoo` endpoint returns structured errors:

**Authorization failures:**
```json
{
  "Authorized": false,
  "Result": null,
  "Error": null
}
```

**Exception during execution:**
```json
{
  "Authorized": true,
  "Result": null,
  "Error": "Exception message"
}
```

Handle errors on the client:

<!-- snippet: aspnetcore-error-handling -->
<a id='snippet-aspnetcore-error-handling'></a>
```cs
public partial class ErrorHandlingExample
{
    // [Fact]
    public async Task HandleNotAuthorizedException()
    {
        var scopes = SampleTestContainers.Scopes();

        // Configure unauthorized user
        var userContext = scopes.server.GetRequiredService<MockUserContext>();
        userContext.IsAuthenticated = false;

        var factory = scopes.client.GetRequiredService<IProtectedServerEntityFactory>();

        try
        {
            factory.Create();
            await Task.CompletedTask;
        }
        catch (NotAuthorizedException ex)
        {
            // Handle authorization failure
            Assert.NotNull(ex.Message);
        }
    }

    // [Fact]
    public async Task HandleValidationException()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IValidatedServerEntityFactory>();

        var entity = factory.Create();
        entity.Name = string.Empty; // Invalid

        try
        {
            await factory.Save(entity);  // Use Save instead of Insert
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            // Handle validation failure
            Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public interface IProtectedAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();
}

public partial class ProtectedAuth : IProtectedAuth
{
    private readonly IUserContext _userContext;
    public ProtectedAuth(IUserContext userContext) { _userContext = userContext; }
    public bool CanCreate() => _userContext.IsAuthenticated;
}

[Factory]
[AuthorizeFactory<IProtectedAuth>]
public partial class ProtectedServerEntity
{
    public Guid Id { get; private set; }

    [Create]
    public ProtectedServerEntity() { Id = Guid.NewGuid(); }
}

[Factory]
public partial class ValidatedServerEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ValidatedServerEntity() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new System.ComponentModel.DataAnnotations.ValidationException("Name is required");
        IsNew = false;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L302-L392' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-error-handling' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## CORS Configuration

Configure CORS for Blazor WASM clients:

<!-- snippet: aspnetcore-cors -->
<a id='snippet-aspnetcore-cors'></a>
```cs
public static class CorsConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        "https://localhost:5001",  // Blazor WASM client
                        "https://myapp.example.com")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();  // Required if using auth cookies
            });

            // Named policy for specific endpoints
            options.AddPolicy("NeatooApi", policy =>
            {
                policy.WithOrigins("https://trusted-client.example.com")
                      .WithHeaders("Content-Type", "X-Correlation-Id")
                      .WithMethods("POST");
            });
        });

        services.AddNeatooAspNetCore(typeof(AspNetCoreIntegrationSamples).Assembly);
    }

    public static void ConfigureApp(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.UseCors(); // Use default policy
        app.UseNeatoo();
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L394-L429' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cors' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Place CORS middleware before `UseNeatoo()` to allow cross-origin requests.

## Minimal API Integration

RemoteFactory integrates seamlessly with minimal APIs:

<!-- snippet: aspnetcore-minimal-api -->
<a id='snippet-aspnetcore-minimal-api'></a>
```cs
public static class MinimalApiIntegration
{
    public static void ConfigureApp(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        // Neatoo coexists with other minimal API endpoints
        app.UseNeatoo(); // POST /api/neatoo

        // Other endpoints
        app.MapGet("/health", () => "OK");

        app.MapGet("/api/info", () => new
        {
            Version = "1.0",
            Framework = "RemoteFactory"
        });

        // app.MapControllers(); // MVC controllers if needed
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L431-L451' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-minimal-api' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `/api/neatoo` endpoint coexists with your other minimal API endpoints.

## Performance Considerations

**Scoped services:**
- Factories and serializers are scoped per-request
- Avoid singleton services that hold request-specific state

**Serialization:**
- Ordinal format reduces payload size by 40-50%
- Use Named format only for debugging

**Event handlers:**
- Run in isolated scopes (new `IServiceScope`)
- Do not block HTTP response (fire-and-forget)
- Respect cancellation tokens for graceful shutdown

## Testing

Test server-side factories using the two-container pattern:

<!-- snippet: aspnetcore-testing -->
<a id='snippet-aspnetcore-testing'></a>
```cs
public partial class TwoContainerTestPattern
{
    // [Fact]
    public async Task ClientServerRoundTrip()
    {
        // Create isolated client/server/local containers
        var scopes = SampleTestContainers.Scopes();

        // Client container - simulates Blazor WASM
        var clientFactory = scopes.client.GetRequiredService<IServerEntityFactory>();

        // Server container - simulates ASP.NET Core server
        // (automatically connected via SampleTestContainers)

        // Local container - for comparison (no serialization)
        var localFactory = scopes.local.GetRequiredService<IServerEntityFactory>();

        // Test client call (goes through serialization)
        var clientEntity = clientFactory.Create();
        Assert.NotNull(clientEntity);

        // Test local call (direct execution)
        var localEntity = localFactory.Create();
        Assert.NotNull(localEntity);

        // Both should produce valid results
        Assert.NotEqual(Guid.Empty, clientEntity.Id);
        Assert.NotEqual(Guid.Empty, localEntity.Id);
        await Task.CompletedTask;
    }

    // [Fact]
    public async Task TestFullWorkflow()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IServerEntityFactory>();

        // Create
        var entity = factory.Create();
        entity.Name = "Integration Test";

        // Save (Insert)
        var saved = await factory.Save(entity);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);

        // Fetch
        var fetched = await factory.Fetch(saved.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Integration Test", fetched.Name);

        // Update
        fetched.Name = "Updated";
        var updated = await factory.Save(fetched);
        Assert.NotNull(updated);

        // Delete
        updated.IsDeleted = true;
        await factory.Save(updated);
    }
}

[Factory]
public partial class ServerEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ServerEntity() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
    {
        Id = id;
        Name = "Fetched";
        IsNew = false;
        return Task.FromResult(true);
    }

    [Remote, Insert]
    public Task Insert([Service] IPersonRepository repository)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update([Service] IPersonRepository repository)
    {
        return Task.CompletedTask;
    }

    [Remote, Delete]
    public Task Delete([Service] IPersonRepository repository)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/AspNetCoreIntegrationSamples.cs#L453-L555' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This simulates client/server communication without HTTP.

See the `RemoteFactory.IntegrationTests` project for comprehensive examples.

## Summary

| Method | Purpose |
|--------|---------|
| `AddNeatooAspNetCore` | Register factories, serialization, and authorization services |
| `UseNeatoo` | Add `/api/neatoo` endpoint middleware |

**Key features:**
- Automatic endpoint generation at `/api/neatoo`
- Correlation ID propagation for distributed tracing
- Integrated logging with `ILoggerFactory`
- Graceful shutdown support for event handlers
- Client disconnect and server shutdown cancellation
- ASP.NET Core authorization integration

## Next Steps

- [Getting Started](getting-started.md) - First working example
- [Authorization](authorization.md) - Authorization patterns
- [Events](events.md) - Fire-and-forget domain events
- [Serialization](serialization.md) - Ordinal vs Named formats
