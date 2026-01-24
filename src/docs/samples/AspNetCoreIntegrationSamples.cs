using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Internal;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/aspnetcore-integration.md documentation.
/// </summary>
public partial class AspNetCoreIntegrationSamples
{
    #region aspnetcore-basic-setup
    // In Program.cs
    public static void ConfigureBasicSetup(IServiceCollection services, WebApplication app)
    {
        // Add Neatoo ASP.NET Core services
        services.AddNeatooAspNetCore(typeof(MyDomainEntity).Assembly);

        // Map Neatoo endpoint at /api/neatoo
        app.UseNeatoo();
    }
    #endregion

    #region aspnetcore-addneatoo
    // Single assembly registration
    public static void RegisterSingleAssembly(IServiceCollection services)
    {
        services.AddNeatooAspNetCore(typeof(MyDomainEntity).Assembly);
    }

    // Multiple assembly registration
    public static void RegisterMultipleAssemblies(IServiceCollection services)
    {
        services.AddNeatooAspNetCore(
            typeof(MyDomainEntity).Assembly,
            typeof(OtherDomainEntity).Assembly);
    }
    #endregion

    #region aspnetcore-custom-serialization
    public static void ConfigureCustomSerialization(IServiceCollection services)
    {
        var options = new NeatooSerializationOptions
        {
            // Named format: traditional JSON objects (larger but readable)
            Format = SerializationFormat.Named
        };

        services.AddNeatooAspNetCore(options, typeof(MyDomainEntity).Assembly);
    }
    #endregion

    #region aspnetcore-middleware-order
    public static void ConfigureMiddlewareOrder(WebApplication app)
    {
        // Middleware order matters
        app.UseCors();              // 1. CORS (if client is cross-origin)
        app.UseAuthentication();    // 2. Authentication (if using [AspAuthorize])
        app.UseAuthorization();     // 3. Authorization
        app.UseNeatoo();            // 4. Neatoo endpoint
    }
    #endregion

    #region aspnetcore-cancellation
    [Factory]
    public partial class LongRunningEntity
    {
        public Guid Id { get; private set; }

        [Create]
        public LongRunningEntity() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public async Task<bool> Fetch(
            Guid id,
            [Service] IPersonRepository repository,
            CancellationToken cancellationToken) // Automatically wired
        {
            // CancellationToken receives signal from:
            // - Client disconnect (HttpContext.RequestAborted)
            // - Server shutdown (IHostApplicationLifetime.ApplicationStopping)

            cancellationToken.ThrowIfCancellationRequested();

            // Pass to async operations
            var entity = await repository.GetByIdAsync(id, cancellationToken);

            Id = id;
            return entity != null;
        }
    }
    #endregion

    #region aspnetcore-correlation-id
    [Factory]
    public partial class AuditedEntity
    {
        public Guid Id { get; private set; }

        [Create]
        public AuditedEntity() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id, [Service] IAuditLogService auditLog)
        {
            // CorrelationContext.CorrelationId is automatically populated from
            // X-Correlation-Id header (or generated if not present)
            var correlationId = CorrelationContext.CorrelationId;

            // Use for distributed tracing
            _ = auditLog.LogAsync("Fetch", id, "Entity", $"Correlation: {correlationId}", default);

            Id = id;
            return Task.FromResult(true);
        }
    }
    #endregion

    #region aspnetcore-logging
    public static void ConfigureNeatooLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);

            // Neatoo-specific log categories:
            // - Neatoo.RemoteFactory.Server - server-side request handling
            // - Neatoo.RemoteFactory.Client - client-side HTTP calls
            // - Neatoo.RemoteFactory.Serialization - serialization details
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Debug);
        });

        services.AddNeatooAspNetCore(typeof(MyDomainEntity).Assembly);
    }
    #endregion

    #region aspnetcore-service-registration
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register Neatoo
        services.AddNeatooAspNetCore(typeof(MyDomainEntity).Assembly);

        // Register domain services (available via [Service] parameters)
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserContext, MockUserContext>();
        services.AddScoped<IEmailService, MockEmailService>();
        services.AddScoped<IAuditLogService, MockAuditLogService>();

        // Auto-register matching interfaces/implementations (IFoo -> Foo)
        services.RegisterMatchingName(typeof(MyDomainEntity).Assembly);
    }
    #endregion

    #region aspnetcore-multi-assembly
    public static void ConfigureMultiAssembly(IServiceCollection services)
    {
        // Register domain assemblies
        services.AddNeatooAspNetCore(
            typeof(MyDomainEntity).Assembly,
            typeof(OtherDomainEntity).Assembly);

        // Register services from all assemblies
        services.RegisterMatchingName(
            typeof(MyDomainEntity).Assembly,
            typeof(OtherDomainEntity).Assembly);
    }
    #endregion

    #region aspnetcore-development
    public static void ConfigureDevelopment(IServiceCollection services)
    {
        // Readable JSON format for debugging
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        services.AddNeatooAspNetCore(options, typeof(MyDomainEntity).Assembly);

        // Detailed logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddFilter("Neatoo", LogLevel.Trace);
        });
    }
    #endregion

    #region aspnetcore-production
    public static void ConfigureProduction(IServiceCollection services)
    {
        // Compact array format for minimal payload size (default)
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };

        services.AddNeatooAspNetCore(options, typeof(MyDomainEntity).Assembly);

        // Minimal logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddFilter("Neatoo", LogLevel.Information);
        });
    }
    #endregion

    #region aspnetcore-error-handling
    // Authorization errors throw NotAuthorizedException
    public static async Task HandleAuthorizationError(IProtectedEntityFactory factory)
    {
        try
        {
            await factory.Fetch(Guid.NewGuid());
        }
        catch (NotAuthorizedException)
        {
            // User not authorized - redirect to login or show error
        }
    }

    // Validation errors throw ValidationException
    public static async Task HandleValidationError(IValidatedEntityFactory factory)
    {
        var entity = factory.Create();
        entity.Name = string.Empty; // Invalid

        try
        {
            await factory.Save(entity);
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            // ex.Message contains validation error details
            var errorMessage = ex.Message; // "Name is required"
        }
    }

    // Supporting types for error handling examples
    public interface IProtectedAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        bool CanFetch();
    }

    public partial class ProtectedAuth : IProtectedAuth
    {
        private readonly IUserContext _userContext;
        public ProtectedAuth(IUserContext userContext) { _userContext = userContext; }
        public bool CanFetch() => _userContext.IsAuthenticated;
    }

    [Factory]
    [AuthorizeFactory<IProtectedAuth>]
    public partial class ProtectedEntity
    {
        public Guid Id { get; private set; }

        [Create]
        public ProtectedEntity() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }

    [Factory]
    public partial class ValidatedEntity : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public ValidatedEntity() { Id = Guid.NewGuid(); }

        [Remote, Insert]
        public Task Insert()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new System.ComponentModel.DataAnnotations.ValidationException("Name is required");
            IsNew = false;
            return Task.CompletedTask;
        }
    }
    #endregion

    #region aspnetcore-cors
    public static void ConfigureCors(IServiceCollection services, WebApplication app)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        "https://localhost:5001",       // Blazor WASM client
                        "https://myapp.example.com")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();              // Required for auth cookies
            });

            // Named policy for specific endpoints
            options.AddPolicy("NeatooApi", policy =>
            {
                policy.WithOrigins("https://trusted-client.example.com")
                      .WithHeaders("Content-Type", "X-Correlation-Id")
                      .WithMethods("POST");
            });
        });

        services.AddNeatooAspNetCore(typeof(MyDomainEntity).Assembly);

        // In Configure:
        app.UseCors();      // Use default policy
        app.UseNeatoo();
    }
    #endregion

    #region aspnetcore-minimal-api
    public static void ConfigureMinimalApi(WebApplication app)
    {
        // Neatoo coexists with other minimal API endpoints
        app.UseNeatoo();  // POST /api/neatoo

        // Other endpoints
        app.MapGet("/health", () => "OK");
        app.MapGet("/api/info", () => new { Version = "1.0", Framework = "RemoteFactory" });
    }
    #endregion

    #region aspnetcore-testing
    // Two-container pattern for testing client/server round-trips
    public partial class TwoContainerTestPattern
    {
        // [Fact]
        public async Task TestClientServerRoundTrip()
        {
            // Create isolated client/server/local containers
            var scopes = SampleTestContainers.Scopes();

            // Client container - simulates Blazor WASM
            var clientFactory = scopes.client.GetRequiredService<ITestEntityFactory>();

            // Local container - for comparison (no serialization)
            var localFactory = scopes.local.GetRequiredService<ITestEntityFactory>();

            // Client call goes through serialization round-trip
            var clientEntity = clientFactory.Create();
            // clientEntity.Id != Guid.Empty

            // Local call executes directly
            var localEntity = localFactory.Create();
            // localEntity.Id != Guid.Empty

            await Task.CompletedTask;
        }

        // [Fact]
        public async Task TestFullCrudWorkflow()
        {
            var scopes = SampleTestContainers.Scopes();
            var factory = scopes.client.GetRequiredService<ITestEntityFactory>();

            // Create
            var entity = factory.Create();
            entity.Name = "Integration Test";

            // Save (routes to Insert because IsNew = true)
            var saved = await factory.Save(entity);
            // saved.IsNew == false

            // Fetch
            var fetched = await factory.Fetch(saved!.Id);
            // fetched.Name == "Integration Test"

            // Update (routes to Update because IsNew = false)
            fetched!.Name = "Updated";
            await factory.Save(fetched);

            // Delete
            fetched.IsDeleted = true;
            await factory.Save(fetched);
        }
    }

    [Factory]
    public partial class TestEntity : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public TestEntity() { Id = Guid.NewGuid(); }

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
    #endregion

    // Placeholder types for documentation examples
    [Factory]
    public partial class MyDomainEntity
    {
        [Create]
        public MyDomainEntity() { }
    }

    [Factory]
    public partial class OtherDomainEntity
    {
        [Create]
        public OtherDomainEntity() { }
    }
}
