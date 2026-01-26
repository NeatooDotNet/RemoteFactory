using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Internal;

namespace EmployeeManagement.Server.WebApi.Samples;

// ASP.NET Core Integration Samples
// These demonstrate server configuration patterns

#region aspnetcore-basic-setup
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
#endregion

#region aspnetcore-addneatoo
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
#endregion

#region aspnetcore-custom-serialization
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
#endregion

#region aspnetcore-middleware-order
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
#endregion

#region aspnetcore-cancellation
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
#endregion

#region aspnetcore-correlation-id
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
#endregion

#region aspnetcore-logging
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
#endregion

#region aspnetcore-service-registration
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
#endregion

#region aspnetcore-multi-assembly
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
#endregion

#region aspnetcore-development
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
#endregion

#region aspnetcore-production
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
#endregion

#region aspnetcore-cors
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
#endregion

#region aspnetcore-minimal-api
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
#endregion

#region aspnetcore-error-handling
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
#endregion

#region service-injection-httpcontext
/// <summary>
/// Demonstrates IHttpContextAccessor injection for HTTP context access.
/// Use this to access request headers, user claims, or other HTTP-specific data.
/// </summary>
[Factory]
public partial class EmployeeHttpContext : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string CreatedBy { get; private set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeHttpContext() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Access HTTP context information via IHttpContextAccessor.
    /// Available only in server-side methods.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        [Service] Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        // Access HTTP context (null in non-HTTP scenarios like testing)
        var httpContext = httpContextAccessor.HttpContext;

        // Get user identity from claims
        CreatedBy = httpContext?.User?.Identity?.Name ?? "system";

        // Access request headers
        var correlationId = httpContext?.Request?.Headers["X-Correlation-ID"].FirstOrDefault();

        // Access other request information
        var userAgent = httpContext?.Request?.Headers.UserAgent.FirstOrDefault();

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
    }
}
#endregion

#region aspnetcore-testing
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
#endregion
