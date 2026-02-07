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
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register RemoteFactory services with the domain assembly
        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        // Map the /api/neatoo endpoint for remote delegate requests
        app.UseNeatoo();

        app.Run();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/BasicSetupSamples.cs#L8-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-basic-setup' title='Start of snippet'>anchor</a></sup>
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
public static class AddNeatooSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register with single domain assembly
        services.AddNeatooAspNetCore(typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/BasicSetupSamples.cs#L28-L37' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-addneatoo' title='Start of snippet'>anchor</a></sup>
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
public static class CustomSerializationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Named format: human-readable JSON (useful for debugging)
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Named },
            typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/BasicSetupSamples.cs#L39-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-custom-serialization' title='Start of snippet'>anchor</a></sup>
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
public static class MiddlewareOrderSample
{
    public static void Configure(WebApplication app)
    {
        app.UseCors();           // 1. CORS first
        app.UseAuthentication(); // 2. Auth middleware
        app.UseAuthorization();
        app.UseNeatoo();         // 3. RemoteFactory endpoint (after auth)
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/BasicSetupSamples.cs#L52-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-middleware-order' title='Start of snippet'>anchor</a></sup>
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
public partial class EmployeeWithCancellation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";

    [Create]
    public EmployeeWithCancellation() => Id = Guid.NewGuid();

    // CancellationToken fires on: client disconnect, server shutdown
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/AspNetCore/CancellationSamples.cs#L7-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Correlation IDs

RemoteFactory automatically manages correlation IDs for distributed tracing.

**Client-side:**
- Generates a unique correlation ID for each request
- Includes it in the `X-Correlation-Id` header

**Server-side:**
- Extracts correlation ID from request headers
- Makes it available via DI-injected `ICorrelationContext`
- Includes it in response headers
- Adds it to structured logs

Access the correlation ID in factory methods:

<!-- snippet: aspnetcore-correlation-id -->
<a id='snippet-aspnetcore-correlation-id'></a>
```cs
[Factory]
public partial class EmployeeWithCorrelation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";

    [Create]
    public EmployeeWithCorrelation() => Id = Guid.NewGuid();

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] ICorrelationContext correlationContext,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        // CorrelationId auto-populated from X-Correlation-Id header
        var correlationId = correlationContext.CorrelationId;

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/AspNetCore/CorrelationIdSamples.cs#L7-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
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
public static class LoggingConfigurationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            // Neatoo categories: Server, Client, Serialization
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Debug);
        });

        services.AddNeatooAspNetCore(typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/LoggingConfigurationSamples.cs#L8-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-logging' title='Start of snippet'>anchor</a></sup>
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
public static class ServiceRegistrationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var assembly = typeof(Employee).Assembly;

        services.AddNeatooAspNetCore(assembly);           // 1. RemoteFactory
        services.RegisterMatchingName(assembly);          // 2. IName -> Name pairs
        // services.AddScoped<ICustom, CustomImpl>();     // 3. Manual registrations
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/ServiceRegistrationSamples.cs#L8-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-service-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**RegisterMatchingName** is a convenience method that registers interfaces and their implementations as transient when they follow the `IName` → `Name` pattern.

Services are injected into factory methods via `[Service]` parameters.

## Multi-Assembly Support

Register factories from multiple assemblies:

<!-- snippet: aspnetcore-multi-assembly -->
<a id='snippet-aspnetcore-multi-assembly'></a>
```cs
public static class MultiAssemblySample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register factories from multiple domain assemblies
        services.AddNeatooAspNetCore(
            typeof(Employee).Assembly
            // , typeof(OtherDomain.Entity).Assembly
        );
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/ServiceRegistrationSamples.cs#L22-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-multi-assembly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each assembly can contain domain models with `[Factory]` attributes. The generator processes all of them.

## Development vs Production

### Development

Use Named serialization format for easier debugging:

<!-- snippet: aspnetcore-development -->
<a id='snippet-aspnetcore-development'></a>
```cs
public static class DevelopmentConfigurationSample
{
    public static void ConfigureServices(IServiceCollection services, bool isDevelopment)
    {
        // Named format for debugging, Ordinal for production
        var format = isDevelopment ? SerializationFormat.Named : SerializationFormat.Ordinal;
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = format },
            typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/EnvironmentConfigurationSamples.cs#L9-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-development' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Production

Use Ordinal format (default) for compact payloads:

<!-- snippet: aspnetcore-production -->
<a id='snippet-aspnetcore-production'></a>
```cs
public static class ProductionConfigurationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Ordinal format: 40-50% smaller payloads (default)
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/EnvironmentConfigurationSamples.cs#L23-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-production' title='Start of snippet'>anchor</a></sup>
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
public class ErrorHandlingSample
{
    public async Task<string> HandleErrorsDemo(IEmployeeFactory factory, Guid employeeId)
    {
        ArgumentNullException.ThrowIfNull(factory);
        try
        {
            var employee = await factory.Fetch(employeeId);
            return employee != null ? $"Found: {employee.FirstName}" : "Not found";
        }
        catch (NotAuthorizedException) { return "Access denied"; }
        catch (HttpRequestException) { return "Server unavailable"; }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Samples/AspNetCore/ErrorHandlingSamples.cs#L6-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-error-handling' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## CORS Configuration

Configure CORS for Blazor WASM clients:

<!-- snippet: aspnetcore-cors -->
<a id='snippet-aspnetcore-cors'></a>
```cs
public static class CorsConfigurationSample
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://client.example.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        app.UseCors();    // CORS must be before UseNeatoo
        app.UseNeatoo();

        app.Run();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/CorsConfigurationSamples.cs#L6-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cors' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Place CORS middleware before `UseNeatoo()` to allow cross-origin requests.

## Minimal API Integration

RemoteFactory integrates seamlessly with minimal APIs:

<!-- snippet: aspnetcore-minimal-api -->
<a id='snippet-aspnetcore-minimal-api'></a>
```cs
public static class MinimalApiSample
{
    public static void Configure(WebApplication app)
    {
        app.UseNeatoo();                           // POST /api/neatoo
        app.MapGet("/health", () => "OK");         // Custom endpoints coexist
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/MinimalApiSamples.cs#L5-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-minimal-api' title='Start of snippet'>anchor</a></sup>
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
public class TwoContainerTestingSample
{
    [Fact]
    public async Task ClientServerRoundTrip()
    {
        // Client, server, local scopes simulate client/server without HTTP
        var (client, server, local) = TestClientServerContainers.CreateScopes();
        var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.Email = new EmailAddress("test@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        await factory.Save(employee);

        var fetched = await factory.Fetch(employee.Id);
        Assert.Equal("Test", fetched?.FirstName);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/AspNetCore/TwoContainerTestingSamples.cs#L8-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-testing' title='Start of snippet'>anchor</a></sup>
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
