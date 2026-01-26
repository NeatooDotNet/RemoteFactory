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
/// <summary>
/// Complete server configuration in a single Program.cs pattern.
/// </summary>
public static class BasicSetupSample
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var domainAssembly = typeof(Employee).Assembly;

        // Register RemoteFactory with ASP.NET Core integration
        builder.Services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory interface -> implementation mappings
        builder.Services.RegisterMatchingName(domainAssembly);

        // Register infrastructure services
        builder.Services.AddInfrastructureServices();
    }

    public static void ConfigureApp(WebApplication app)
    {
        // Configure the /api/neatoo endpoint
        app.UseNeatoo();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L14-L43' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-basic-setup' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// AddNeatooAspNetCore registration with domain assembly.
/// </summary>
public static class AddNeatooSample
{
    public static void Configure(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Registers:
        // - Generated factory interfaces and implementations (scoped)
        // - NeatooJsonSerializer and NeatooSerializationOptions
        // - IAspAuthorize default implementation
        // - HandleRemoteDelegateRequest
        // - EventTrackerHostedService for graceful shutdown
        services.AddNeatooAspNetCore(domainAssembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L45-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-addneatoo' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// AddNeatooAspNetCore with custom serialization options.
/// </summary>
public static class CustomSerializationSample
{
    public static void Configure(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Use Named format for debugging (human-readable JSON)
        // Must match client format
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Named },
            domainAssembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L66-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-custom-serialization' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Proper middleware ordering with UseNeatoo.
/// </summary>
public static class MiddlewareOrderSample
{
    public static void Configure(WebApplication app)
    {
        // 1. CORS - Allow cross-origin requests
        app.UseCors();

        // 2. Authentication - Establish identity
        app.UseAuthentication();

        // 3. Authorization - Check permissions
        app.UseAuthorization();

        // 4. UseNeatoo - RemoteFactory endpoint at /api/neatoo
        // Must be after auth middleware for [AspAuthorize] to work
        app.UseNeatoo();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L85-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-middleware-order' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Factory method demonstrating cancellation support.
/// CancellationToken is linked to HttpContext.RequestAborted.
/// </summary>
[Factory]
public partial class EmployeeCancellationDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeCancellationDemo() { Id = Guid.NewGuid(); }

    /// <summary>
    /// CancellationToken receives cancellation from:
    /// - Client disconnect (HttpContext.RequestAborted)
    /// - Server shutdown (IHostApplicationLifetime.ApplicationStopping)
    /// - Explicit cancellation from caller
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        // Check cancellation before expensive work
        ct.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(repository);
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        // Pass token to all async operations
        ct.ThrowIfCancellationRequested();

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L109-L153' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cancellation' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Factory method accessing correlation ID for distributed tracing.
/// </summary>
[Factory]
public partial class EmployeeCorrelationDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeCorrelationDemo() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Access correlation ID via static CorrelationContext.
    /// Note: This API may be redesigned to support DI in a future version.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        [Service] ILogger<EmployeeCorrelationDemo> logger,
        CancellationToken ct)
    {
        // Access correlation ID from static context
        var correlationId = CorrelationContext.CorrelationId;

        // Include in structured logs
        logger.LogInformation(
            "Fetching employee {EmployeeId} with correlation {CorrelationId}",
            id, correlationId);

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L155-L198' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Factory method with structured logging integration.
/// </summary>
[Factory]
public partial class EmployeeLoggingDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeLoggingDemo() { Id = Guid.NewGuid(); }

    /// <summary>
    /// ILogger injected via [Service] for structured logging.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        [Service] ILogger<EmployeeLoggingDemo> logger,
        CancellationToken ct)
    {
        var correlationId = CorrelationContext.CorrelationId;

        logger.LogInformation(
            "Creating employee {EmployeeName} with correlation {CorrelationId}",
            FirstName, correlationId);

        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;

        // Log successful creation
        // logger.LogInformation("Employee {EmployeeId} created successfully", Id);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L200-L246' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-logging' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Complete service registration pattern.
/// </summary>
public static class ServiceRegistrationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // RemoteFactory with ASP.NET Core integration
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Auto-register IName -> Name interface/implementation pairs
        services.RegisterMatchingName(domainAssembly);

        // Register infrastructure services
        services.AddInfrastructureServices();

        // Manual service registration for services not following IName pattern
        // services.AddScoped<ISpecialService, SpecialServiceImpl>();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L248-L273' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-service-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**RegisterMatchingName** is a convenience method that registers interfaces and their implementations as transient when they follow the `IName` → `Name` pattern.

Services are injected into factory methods via `[Service]` parameters.

## Multi-Assembly Support

Register factories from multiple assemblies:

<!-- snippet: aspnetcore-multi-assembly -->
<a id='snippet-aspnetcore-multi-assembly'></a>
```cs
/// <summary>
/// Registering multiple domain assemblies.
/// </summary>
public static class MultiAssemblySample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register factories from multiple assemblies
        // Each assembly can contain domain models with [Factory] attributes
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(Employee).Assembly           // EmployeeManagement.Domain
            // , typeof(OtherModel).Assembly    // Other domain assemblies
        );
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L275-L292' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-multi-assembly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each assembly can contain domain models with `[Factory]` attributes. The generator processes all of them.

## Development vs Production

### Development

Use Named serialization format for easier debugging:

<!-- snippet: aspnetcore-development -->
<a id='snippet-aspnetcore-development'></a>
```cs
/// <summary>
/// Development environment configuration.
/// </summary>
public static class DevelopmentConfigSample
{
    public static void Configure(WebApplicationBuilder builder)
    {
        var domainAssembly = typeof(Employee).Assembly;

        ArgumentNullException.ThrowIfNull(builder);
        if (builder.Environment.IsDevelopment())
        {
            // Named format for human-readable JSON in dev tools
            builder.Services.AddNeatooAspNetCore(
                new NeatooSerializationOptions { Format = SerializationFormat.Named },
                domainAssembly);

            // Verbose logging for debugging
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Logging.AddFilter("Neatoo.RemoteFactory", LogLevel.Debug);
        }
        else
        {
            // Production: Ordinal format for compact payloads
            builder.Services.AddNeatooAspNetCore(
                new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
                domainAssembly);
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L294-L325' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-development' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Production

Use Ordinal format (default) for compact payloads:

<!-- snippet: aspnetcore-production -->
<a id='snippet-aspnetcore-production'></a>
```cs
/// <summary>
/// Production environment configuration.
/// </summary>
public static class ProductionConfigSample
{
    public static void Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var domainAssembly = typeof(Employee).Assembly;

        // Ordinal format for 40-50% smaller payloads
        builder.Services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Production logging levels
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter("Neatoo.RemoteFactory", LogLevel.Warning);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L327-L348' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-production' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Client-side error handling for remote calls.
/// Factory methods throw exceptions - they don't return RemoteResponse.
/// </summary>
public static class ErrorHandlingSample
{
    public static async Task<string> SafeFetchEmployee(
        IEmployeeFactory factory,
        Guid employeeId)
    {
        ArgumentNullException.ThrowIfNull(factory);
        try
        {
            // Factory methods return domain objects, not RemoteResponse
            var employee = await factory.Fetch(employeeId);
            if (employee == null)
            {
                return "Employee not found";
            }
            return $"Found: {employee.FirstName} {employee.LastName}";
        }
        catch (NotAuthorizedException ex)
        {
            // User lacks permission
            return $"Authorization failed: {ex.Message}";
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            // Data validation failed
            return $"Validation failed: {ex.Message}";
        }
        catch (OperationCanceledException)
        {
            // Request was cancelled
            return "Request cancelled";
        }
        catch (HttpRequestException ex)
        {
            // Network errors
            return $"Network error: {ex.Message}";
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L401-L445' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-error-handling' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## CORS Configuration

Configure CORS for Blazor WASM clients:

<!-- snippet: aspnetcore-cors -->
<a id='snippet-aspnetcore-cors'></a>
```cs
/// <summary>
/// CORS configuration for Blazor WASM clients.
/// </summary>
public static class CorsSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()    // Or WithOrigins("https://client.example.com")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    public static void ConfigureApp(WebApplication app)
    {
        // CORS must be before UseNeatoo for cross-origin requests
        app.UseCors();
        app.UseNeatoo();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L350-L376' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cors' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Place CORS middleware before `UseNeatoo()` to allow cross-origin requests.

## Minimal API Integration

RemoteFactory integrates seamlessly with minimal APIs:

<!-- snippet: aspnetcore-minimal-api -->
<a id='snippet-aspnetcore-minimal-api'></a>
```cs
/// <summary>
/// RemoteFactory with minimal API endpoints.
/// </summary>
public static class MinimalApiSample
{
    public static void ConfigureApp(WebApplication app)
    {
        // RemoteFactory endpoint at /api/neatoo
        app.UseNeatoo();

        // Custom minimal API endpoints coexist with RemoteFactory
        app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy" }));

        app.MapGet("/api/employees/{id:guid}", async (Guid id, IEmployeeRepository repo) =>
        {
            var employee = await repo.GetByIdAsync(id, default);
            return employee is not null ? Results.Ok(employee) : Results.NotFound();
        });
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L378-L399' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-minimal-api' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Two-container testing pattern for client-server simulation.
/// Tests RemoteFactory operations without HTTP using isolated DI scopes.
/// </summary>
public static class AspNetCoreTestingSample
{
    /// <summary>
    /// Create isolated client, server, and local containers for testing.
    /// This validates serialization round-trip without HTTP overhead.
    /// </summary>
    public static void TestWithTwoContainers()
    {
        // Create isolated scopes simulating client and server containers
        // var (client, server, local) = TestClientServerContainers.CreateScopes();
        //
        // Client container: NeatooFactory.Remote mode (makes serialized calls)
        // Server container: NeatooFactory.Server mode (handles serialized calls)
        // Local container: NeatooFactory.Logical mode (direct execution for comparison)
        //
        // Example test flow:
        // 1. Get factory from local container
        // var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();
        //
        // 2. Create and save
        // var employee = factory.Create();
        // employee.FirstName = "Test";
        // await factory.Save(employee);
        //
        // 3. Fetch verifies serialization round-trip
        // var fetched = await factory.Fetch(employee.Id);
        // Assert.Equal("Test", fetched.FirstName);
        //
        // See RemoteFactory.IntegrationTests for comprehensive examples
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L501-L537' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-testing' title='Start of snippet'>anchor</a></sup>
<a id='snippet-aspnetcore-testing-1'></a>
```cs
/// <summary>
/// Two-container testing pattern for client-server simulation.
/// </summary>
public class TwoContainerTestingSample
{
    [Fact]
    public async Task ClientServerRoundTrip()
    {
        // Arrange - Create isolated client, server, and local scopes
        var (client, server, local) = TestClientServerContainers.CreateScopes();

        // In Logical mode, all containers share the same implementation
        // For full client-server testing, use separate containers with
        // Remote mode on client and Server mode on server

        var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Act - Create and persist
        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "RoundTrip";
        employee.Email = new EmailAddress("test.roundtrip@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        await factory.Save(employee);

        // Assert - Fetch returns persisted data
        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Test", fetched.FirstName);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L523-L558' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-testing-1' title='Start of snippet'>anchor</a></sup>
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
