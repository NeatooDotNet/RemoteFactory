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

        // Register with multiple assemblies (if your domain spans multiple projects):
        // services.AddNeatooAspNetCore(
        //     typeof(Employee).Assembly,
        //     typeof(Department).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/BasicSetupSamples.cs#L28-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-addneatoo' title='Start of snippet'>anchor</a></sup>
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
        // Named format produces larger but more readable JSON (useful for debugging)
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/BasicSetupSamples.cs#L44-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-custom-serialization' title='Start of snippet'>anchor</a></sup>
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
        // 1. CORS - must be first for cross-origin requests
        app.UseCors();

        // 2. Authentication/Authorization - before protected endpoints
        app.UseAuthentication();
        app.UseAuthorization();

        // 3. UseNeatoo - the /api/neatoo endpoint
        app.UseNeatoo();

        // 4. Other endpoints (controllers, minimal APIs, etc.)
        // app.MapControllers();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/BasicSetupSamples.cs#L60-L79' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-middleware-order' title='Start of snippet'>anchor</a></sup>
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
/// Employee with cancellation support for long-running operations.
/// </summary>
[Factory]
public partial class EmployeeWithCancellation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EmailAddress Email { get; set; } = null!;

    [Create]
    public EmployeeWithCancellation()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an Employee with cancellation support.
    /// Cancellation fires on: 1. Client disconnect, 2. Server shutdown
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        // Check for cancellation before starting work
        cancellationToken.ThrowIfCancellationRequested();

        // Pass token to async operations for cooperative cancellation
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
            return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = new EmailAddress(entity.Email);
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/AspNetCore/CancellationSamples.cs#L7-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cancellation' title='Start of snippet'>anchor</a></sup>
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
/// Employee with correlation ID support for distributed tracing.
/// </summary>
[Factory]
public partial class EmployeeWithCorrelation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EmailAddress Email { get; set; } = null!;

    [Create]
    public EmployeeWithCorrelation()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an Employee and logs the access with correlation ID for tracing.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // CorrelationId is auto-populated from X-Correlation-Id header
        var correlationId = CorrelationContext.CorrelationId;

        var entity = await repository.GetByIdAsync(id, ct);

        if (entity == null)
            return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = new EmailAddress(entity.Email);

        // Log with correlation ID for distributed tracing
        await auditLog.LogAsync(
            action: "Fetch",
            entityId: Id,
            entityType: nameof(EmployeeWithCorrelation),
            details: $"Fetched by correlation: {correlationId}",
            ct);

        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/AspNetCore/CorrelationIdSamples.cs#L8-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
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
        // Configure logging with Neatoo categories
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);

            // Neatoo log categories:
            // - Neatoo.RemoteFactory.Server: Remote delegate execution
            // - Neatoo.RemoteFactory.Client: HTTP client requests
            // - Neatoo.RemoteFactory.Serialization: JSON serialization/deserialization
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Debug);
        });

        // Add RemoteFactory after logging is configured
        services.AddNeatooAspNetCore(typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/LoggingConfigurationSamples.cs#L9-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-logging' title='Start of snippet'>anchor</a></sup>
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
        var domainAssembly = typeof(Employee).Assembly;

        // 1. Register RemoteFactory services first
        services.AddNeatooAspNetCore(domainAssembly);

        // 2. Register domain repositories
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IDepartmentRepository, InMemoryDepartmentRepository>();

        // 3. Register infrastructure services
        services.AddScoped<IUserContext, DefaultUserContext>();
        services.AddScoped<IEmailService, InMemoryEmailService>();
        services.AddScoped<IAuditLogService, InMemoryAuditLogService>();

        // 4. Auto-register IName/Name pattern as transient
        // Services are available via [Service] parameters in factory methods
        services.RegisterMatchingName(domainAssembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/ServiceRegistrationSamples.cs#L11-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-service-registration' title='Start of snippet'>anchor</a></sup>
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
        // Primary domain assembly
        var employeeDomainAssembly = typeof(Employee).Assembly;

        // Additional assemblies containing [Factory] types:
        // var hrDomainAssembly = typeof(HR.Domain.HrEntity).Assembly;
        // var payrollDomainAssembly = typeof(Payroll.Domain.PayrollEntity).Assembly;

        // Register all assemblies with RemoteFactory
        services.AddNeatooAspNetCore(
            employeeDomainAssembly
            // hrDomainAssembly,
            // payrollDomainAssembly
        );

        // Auto-register services from all assemblies
        services.RegisterMatchingName(
            employeeDomainAssembly
            // hrDomainAssembly,
            // payrollDomainAssembly
        );
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/ServiceRegistrationSamples.cs#L37-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-multi-assembly' title='Start of snippet'>anchor</a></sup>
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
        // Choose format based on environment
        var format = isDevelopment
            ? SerializationFormat.Named   // Readable JSON for debugging
            : SerializationFormat.Ordinal; // Compact arrays for production

        var options = new NeatooSerializationOptions { Format = format };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);

        // Enable verbose logging in development
        if (isDevelopment)
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
            });
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/EnvironmentConfigurationSamples.cs#L9-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-development' title='Start of snippet'>anchor</a></sup>
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
        // Ordinal format for minimal payload size (default)
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };

        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);

        // Production logging - less verbose
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Information);
        });
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/EnvironmentConfigurationSamples.cs#L35-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-production' title='Start of snippet'>anchor</a></sup>
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
    private readonly IEmployeeFactory _factory;

    public ErrorHandlingSample(IEmployeeFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> HandleErrorsDemo(Guid employeeId)
    {
        try
        {
            // Fetch operation that may fail with authorization or server errors
            var employee = await _factory.Fetch(employeeId);

            if (employee == null)
                return "Employee not found";

            return $"Found: {employee.FirstName} {employee.LastName}";
        }
        catch (NotAuthorizedException ex)
        {
            // Authorization failed - user doesn't have permission
            // Occurs when [AspAuthorize] policy denies access
            return $"Access denied: {ex.Message}";
        }
        catch (Exception ex) when (ex.Message.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            // Server-side validation failed
            // Occurs when domain rules reject the operation
            return $"Validation failed: {ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            // Network or server connectivity issue
            return $"Server unavailable: {ex.Message}";
        }
    }

    public async Task<string> CreateWithErrorHandling(string firstName, string lastName, string email)
    {
        try
        {
            var employee = _factory.Create();
            employee.FirstName = firstName;
            employee.LastName = lastName;
            employee.Email = new Domain.ValueObjects.EmailAddress(email);

            var saved = await _factory.Save(employee);
            return saved != null
                ? $"Created employee: {saved.Id}"
                : "Failed to create employee";
        }
        catch (NotAuthorizedException)
        {
            // User not authorized to create employees
            return "You don't have permission to create employees";
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Samples/AspNetCore/ErrorHandlingSamples.cs#L7-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-error-handling' title='Start of snippet'>anchor</a></sup>
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

        // Configure default CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5001",  // Development
                        "https://myapp.example.com" // Production
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Required for auth cookies
            });

            // Named policy with specific headers for Neatoo API
            options.AddPolicy("NeatooApi", policy =>
            {
                policy.WithOrigins("http://localhost:5001")
                    .WithHeaders("Content-Type", "X-Correlation-Id")
                    .WithMethods("POST");
            });
        });

        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        // CORS must come before UseNeatoo
        app.UseCors();
        app.UseNeatoo();

        app.Run();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/CorsConfigurationSamples.cs#L7-L48' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cors' title='Start of snippet'>anchor</a></sup>
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
        // RemoteFactory endpoint: POST /api/neatoo
        app.UseNeatoo();

        // Health check endpoint
        app.MapGet("/health", () => "OK");

        // Custom API endpoint alongside Neatoo
        app.MapGet("/api/info", () => new
        {
            Version = "1.0.0",
            Framework = RuntimeInformation.FrameworkDescription
        });

        // MVC controllers (optional)
        // app.MapControllers();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/MinimalApiSamples.cs#L6-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-minimal-api' title='Start of snippet'>anchor</a></sup>
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
/// Two-container test pattern for client/server simulation.
/// </summary>
public class TwoContainerTestingSample
{
    [Fact]
    public void ClientServerRoundTrip_CompareClientVsLocalFactory()
    {
        // Arrange - Get scopes from test container helper
        var (client, server, local) = TestClientServerContainers.CreateScopes();

        // Client container simulates Blazor WASM
        var clientFactory = client.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Local container has no serialization (Logical mode)
        var localFactory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Act - Create entities from both containers
        var clientEmployee = clientFactory.Create();
        var localEmployee = localFactory.Create();

        // Assert - Both should produce valid entities
        Assert.NotEqual(Guid.Empty, clientEmployee.Id);
        Assert.NotEqual(Guid.Empty, localEmployee.Id);
        Assert.True(clientEmployee.IsNew);
        Assert.True(localEmployee.IsNew);
    }

    [Fact]
    public async Task TestFullWorkflow_CreateSaveFetchUpdateDelete()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create
        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Employee";
        employee.Email = new EmailAddress("test@example.com");
        employee.Position = "Tester";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        Assert.True(employee.IsNew);

        // Save (Insert)
        var saved = await factory.Save(employee);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);

        // Fetch
        var fetched = await factory.Fetch(saved.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Test", fetched.FirstName);

        // Update
        fetched.FirstName = "Updated";
        var updated = await factory.Save(fetched);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.FirstName);

        // Delete
        updated.IsDeleted = true;
        await factory.Save(updated);

        // Verify deletion
        var deleted = await factory.Fetch(updated.Id);
        Assert.Null(deleted);
    }
}

/// <summary>
/// Employee entity with full CRUD for testing.
/// </summary>
[Factory]
public partial class EmployeeForTest : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeForTest()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var parts = Name.Split(' ', 2);
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = parts.Length > 0 ? parts[0] : "",
            LastName = parts.Length > 1 ? parts[1] : "",
            Email = "test@example.com",
            Position = "Test",
            SalaryAmount = 50000,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            var parts = Name.Split(' ', 2);
            entity.FirstName = parts.Length > 0 ? parts[0] : "";
            entity.LastName = parts.Length > 1 ? parts[1] : "";
            await repository.UpdateAsync(entity, ct);
            await repository.SaveChangesAsync(ct);
        }
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/AspNetCore/TwoContainerTestingSamples.cs#L10-L161' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-testing' title='Start of snippet'>anchor</a></sup>
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
