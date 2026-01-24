using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/factory-modes.md documentation.
/// </summary>
public partial class FactoryModesSamples
{
    #region modes-full-config
    // Full mode (compile-time) is the default - no attribute needed
    // Or explicitly: [assembly: FactoryMode(FactoryMode.Full)]

    public static class FullModeConfiguration
    {
        public static void ConfigureServer(IServiceCollection services)
        {
            // Full mode assembly + Server runtime mode
            // Full mode generates both local execution and remote handler code
            services.AddNeatooRemoteFactory(
                NeatooFactory.Server, // Runtime mode: handles incoming HTTP
                typeof(FactoryModesSamples).Assembly);
        }
    }
    #endregion

    #region modes-full-generated
    // In Full mode, the generated factory has:
    // 1. Local execution path - calls entity methods directly
    // 2. Remote execution path - serializes and sends via HTTP (when IMakeRemoteDelegateRequest is registered)
    //
    // Generated factory (simplified):
    // public partial class EntityFactory : IEntityFactory
    // {
    //     private readonly IMakeRemoteDelegateRequest? _remoteRequest;
    //
    //     public async Task<Entity> Create()
    //     {
    //         if (_remoteRequest != null)
    //         {
    //             // Remote execution - serialize and send
    //             return await _remoteRequest.ForDelegate<Entity>(...);
    //         }
    //         else
    //         {
    //             // Local execution - call directly
    //             return new Entity();
    //         }
    //     }
    // }
    #endregion

    #region modes-remoteonly-config
    // In client assembly's AssemblyAttributes.cs:
    // [assembly: FactoryMode(FactoryMode.RemoteOnly)]

    public static class RemoteOnlyConfiguration
    {
        public static void ConfigureClient(IServiceCollection services, string serverUrl)
        {
            // RemoteOnly mode assembly + Remote runtime mode
            // RemoteOnly generates HTTP stubs only (smaller assembly)
            services.AddNeatooRemoteFactory(
                NeatooFactory.Remote, // Runtime mode: makes HTTP calls to server
                typeof(FactoryModesSamples).Assembly);

            // Must register HttpClient for remote calls
            services.AddKeyedScoped(
                RemoteFactoryServices.HttpClientKey,
                (sp, key) => new HttpClient { BaseAddress = new Uri(serverUrl) });
        }
    }
    #endregion

    #region modes-remoteonly-generated
    // In RemoteOnly mode, the generated factory only has HTTP stubs:
    //
    // Generated factory (simplified):
    // public partial class EntityFactory : IEntityFactory
    // {
    //     private readonly IMakeRemoteDelegateRequest _remoteRequest;
    //
    //     public async Task<Entity> Create()
    //     {
    //         // Always remote - serialize and send via HTTP
    //         return await _remoteRequest.ForDelegate<Entity>(...);
    //     }
    // }
    //
    // Benefits:
    // - Smaller assembly size (no local implementation code)
    // - No server-side dependencies (DbContext, repositories, etc.)
    // - Clear client/server separation
    #endregion

    #region modes-server-config
    public static class ServerModeConfiguration
    {
        public static void ConfigureServer(IServiceCollection services)
        {
            // Server mode - handles incoming HTTP requests from clients
            // AddNeatooAspNetCore uses NeatooFactory.Server internally
            services.AddNeatooAspNetCore(typeof(FactoryModesSamples).Assembly);

            // Register server-side services (repositories, DbContext, etc.)
            services.AddScoped<IPersonRepository, PersonRepository>();
        }
    }
    #endregion

    #region modes-remote-config
    public static class RemoteModeConfiguration
    {
        public static void ConfigureClient(IServiceCollection services, string serverUrl)
        {
            // Remote mode - all factory operations go via HTTP to server
            services.AddNeatooRemoteFactory(
                NeatooFactory.Remote,
                typeof(FactoryModesSamples).Assembly);

            // Register HttpClient with the server's base address
            services.AddKeyedScoped(
                RemoteFactoryServices.HttpClientKey,
                (sp, key) => new HttpClient { BaseAddress = new Uri(serverUrl) });
        }
    }
    #endregion

    #region modes-logical-config
    public static class LogicalModeConfiguration
    {
        public static void ConfigureLogical(IServiceCollection services)
        {
            // Logical mode - direct execution, no serialization
            // Use for single-tier apps or unit tests
            services.AddNeatooRemoteFactory(
                NeatooFactory.Logical,
                typeof(FactoryModesSamples).Assembly);
        }
    }
    #endregion

    #region modes-logical-testing
    public partial class LogicalModeTestingExample
    {
        // [Fact]
        public async Task TestDomainLogic_WithoutHttp()
        {
            // Create container in Logical mode
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostApplicationLifetime, TestHostApplicationLifetime>();

            // Logical mode - direct execution
            services.AddNeatooRemoteFactory(
                NeatooFactory.Logical,
                typeof(FactoryModesSamples).Assembly);

            // Register domain services
            services.AddSingleton<IPersonRepository, PersonRepository>();

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeEntityFactory>();

            // Test domain logic without HTTP overhead
            var entity = factory.Create();
            entity.Name = "Test";

            // Method executes directly, no serialization
            await factory.Fetch(Guid.NewGuid());
        }
    }
    #endregion

    [Factory]
    public partial class LogicalModeEntity
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;

        [Create]
        public LogicalModeEntity() { Id = Guid.NewGuid(); }

        [Fetch]
        public Task Fetch(Guid id)
        {
            Id = id;
            return Task.CompletedTask;
        }
    }

    #region modes-full-example
    // Complete server setup with Full mode

    [Factory]
    public partial class ServerModeEntity : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public ServerModeEntity() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public async Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
        {
            var entity = await repository.GetByIdAsync(id);
            if (entity == null) return false;

            Id = entity.Id;
            Name = entity.FirstName;
            IsNew = false;
            return true;
        }

        [Remote, Insert]
        public async Task Insert([Service] IPersonRepository repository)
        {
            await repository.AddAsync(new PersonEntity
            {
                Id = Id,
                FirstName = Name,
                LastName = string.Empty,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            });
            await repository.SaveChangesAsync();
            IsNew = false;
        }

        [Remote, Update]
        public async Task Update([Service] IPersonRepository repository)
        {
            var entity = await repository.GetByIdAsync(Id);
            if (entity != null)
            {
                entity.FirstName = Name;
                entity.Modified = DateTime.UtcNow;
                await repository.UpdateAsync(entity);
                await repository.SaveChangesAsync();
            }
        }
    }

    public static class FullModeServerSetup
    {
        public static void Configure(IServiceCollection services)
        {
            // Server mode - handles incoming HTTP requests
            services.AddNeatooAspNetCore(typeof(FactoryModesSamples).Assembly);

            // Register server-only services
            services.AddScoped<IPersonRepository, PersonRepository>();
        }
    }
    #endregion

    #region modes-remoteonly-example
    // Complete client setup with RemoteOnly mode

    public static class RemoteOnlyClientSetup
    {
        public static void Configure(IServiceCollection services, string serverUrl)
        {
            // Remote mode - all operations go via HTTP
            services.AddNeatooRemoteFactory(
                NeatooFactory.Remote,
                typeof(FactoryModesSamples).Assembly);

            // Register keyed HttpClient
            services.AddKeyedScoped(
                RemoteFactoryServices.HttpClientKey,
                (sp, key) => new HttpClient
                {
                    BaseAddress = new Uri(serverUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                });

            // Client-side services only
            services.AddSingleton<IClientLoggerService, ClientLoggerService>();
        }
    }

    public interface IClientLoggerService
    {
        void Log(string message);
    }

    public partial class ClientLoggerService : IClientLoggerService
    {
        public void Log(string message) => Console.WriteLine(message);
    }
    #endregion

    #region modes-logical-example
    // Complete single-tier setup with Logical mode

    public static class LogicalModeSetup
    {
        public static void Configure(IServiceCollection services)
        {
            // Logical mode - direct execution, no HTTP
            services.AddNeatooRemoteFactory(
                NeatooFactory.Logical,
                typeof(FactoryModesSamples).Assembly);

            // All services available locally
            services.AddSingleton<IPersonRepository, PersonRepository>();
            services.AddSingleton<IOrderRepository, OrderRepository>();
        }
    }

    public partial class SingleTierAppExample
    {
        // [Fact]
        public async Task RunLocally()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostApplicationLifetime, TestHostApplicationLifetime>();
            LogicalModeSetup.Configure(services);

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeEntityFactory>();

            var entity = factory.Create();
            entity.Name = "Local Entity";

            // Executes directly - no HTTP, no serialization
            Assert.NotNull(entity);
            Assert.Equal("Local Entity", entity.Name);
            await Task.CompletedTask;
        }
    }
    #endregion

    #region modes-local-remote-methods
    [Factory]
    public partial class MixedModeEntity
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public string LocalOnlyData { get; private set; } = string.Empty;
        public string RemoteData { get; private set; } = string.Empty;

        [Create]
        public MixedModeEntity() { Id = Guid.NewGuid(); }

        // Local-only method - executes on client/server directly
        // No [Remote] attribute means this only runs locally
        [Fetch]
        public void FetchLocal(string data)
        {
            LocalOnlyData = data;
        }

        // Remote method - serializes and executes on server
        [Remote]
        [Fetch]
        public async Task FetchRemote(Guid id, [Service] IPersonRepository repository)
        {
            var entity = await repository.GetByIdAsync(id);
            if (entity != null)
            {
                Id = entity.Id;
                Name = entity.FirstName;
                RemoteData = "Loaded from server";
            }
        }
    }
    #endregion

    #region modes-logging
    public static class ModeLogging
    {
        public static void ConfigureWithLogging(IServiceCollection services, NeatooFactory mode)
        {
            // Configure logging to see mode behavior
            // services.AddLogging(builder =>
            // {
            //     builder.AddConsole();
            //     builder.SetMinimumLevel(LogLevel.Debug);
            //     builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
            // });

            services.AddNeatooRemoteFactory(mode, typeof(FactoryModesSamples).Assembly);
        }
    }

    // Logs will show:
    // - "Executing local factory method..." for Server/Logical modes
    // - "Sending remote factory request..." for Remote mode
    // - Serialization format and payload size
    #endregion

    // [Fact]
    public async Task FullMode_LocalExecution()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.server.GetRequiredService<IServerModeEntityFactory>();

        var entity = factory.Create();
        entity.Name = "Server Test";

        var saved = await factory.Save(entity);
        Assert.NotNull(saved);
    }

    // [Fact]
    public async Task RemoteMode_ClientExecution()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IServerModeEntityFactory>();

        // Client call goes through simulated HTTP
        var entity = factory.Create();
        entity.Name = "Client Test";

        var saved = await factory.Save(entity);
        Assert.NotNull(saved);
    }

    // [Fact]
    public void LogicalMode_DirectExecution()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ILogicalModeEntityFactory>();

        var entity = factory.Create();
        entity.Name = "Logical Test";

        Assert.NotNull(entity);
        Assert.Equal("Logical Test", entity.Name);
    }
}
