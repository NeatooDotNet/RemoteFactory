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
    #endregion

    #region aspnetcore-addneatoo
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
    #endregion

    #region aspnetcore-custom-serialization
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
    #endregion

    #region aspnetcore-middleware-order
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
    #endregion

    #region aspnetcore-cancellation
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
    #endregion

    #region aspnetcore-correlation-id
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
    #endregion

    #region aspnetcore-logging
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
    #endregion

    #region aspnetcore-service-registration
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
    #endregion

    #region aspnetcore-multi-assembly
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
    #endregion

    #region aspnetcore-development
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
    #endregion

    #region aspnetcore-production
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
    #endregion

    #region aspnetcore-error-handling
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
    #endregion

    #region aspnetcore-cors
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
    #endregion

    #region aspnetcore-minimal-api
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
    #endregion

    #region aspnetcore-testing
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
    #endregion

    // [Fact]
    public async Task BasicSetup_Works()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.server.GetRequiredService<IServerEntityFactory>();

        var entity = factory.Create();
        Assert.NotNull(entity);
        await Task.CompletedTask; // Satisfy async signature
    }

    // [Fact]
    public async Task ClientServerRoundTrip_Works()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IServerEntityFactory>();

        var entity = factory.Create();
        entity.Name = "Test";

        var saved = await factory.Save(entity);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
    }
}
