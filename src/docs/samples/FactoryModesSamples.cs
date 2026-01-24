using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/factory-modes.md documentation.
/// </summary>
public partial class FactoryModesSamples
{
    #region modes-full-config
    // Full mode is the default - no assembly attribute needed
    // Or explicitly: [assembly: FactoryMode(FactoryMode.Full)]

    public void ConfigureFullMode(IServiceCollection services)
    {
        // Server runtime mode with Full compile-time mode
        services.AddNeatooRemoteFactory(
            NeatooFactory.Server, // Handles incoming HTTP requests
            typeof(FactoryModesSamples).Assembly);
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

    public void ConfigureRemoteOnlyMode(IServiceCollection services)
    {
        // Remote runtime mode - all operations go via HTTP
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(FactoryModesSamples).Assembly);

        // Register HttpClient for remote calls
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient { BaseAddress = new Uri("https://api.example.com") });
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
    public void ConfigureServerMode(IServiceCollection services)
    {
        // AddNeatooAspNetCore uses NeatooFactory.Server internally
        services.AddNeatooAspNetCore(typeof(FactoryModesSamples).Assembly);

        // Register server-side services
        services.AddScoped<IPersonRepository, PersonRepository>();
    }
    #endregion

    #region modes-remote-config
    public void ConfigureRemoteMode(IServiceCollection services)
    {
        // Remote mode - all factory operations go via HTTP to server
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(FactoryModesSamples).Assembly);

        // Register HttpClient with the server's base address
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient { BaseAddress = new Uri("https://api.example.com") });
    }
    #endregion

    #region modes-logical-config
    public void ConfigureLogicalMode(IServiceCollection services)
    {
        // Logical mode - direct execution, no serialization
        // Use for single-tier apps or unit tests
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            typeof(FactoryModesSamples).Assembly);
    }
    #endregion

    #region modes-logical-testing
    public async Task TestWithLogicalMode()
    {
        // Logical mode enables testing without HTTP overhead
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostApplicationLifetime, TestHostApplicationLifetime>();
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(FactoryModesSamples).Assembly);
        services.AddSingleton<IPersonRepository, PersonRepository>();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ILogicalModeEntityFactory>();

        // Methods execute directly - no serialization, no HTTP
        var entity = factory.Create();
        entity.Name = "Test";
        await factory.Fetch(Guid.NewGuid()); // Runs locally
    }
    #endregion

    #region modes-full-example
    // Complete server entity with Full mode

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

    // Server configuration:
    // services.AddNeatooAspNetCore(typeof(ServerModeEntity).Assembly);
    // services.AddScoped<IPersonRepository, PersonRepository>();
    #endregion

    #region modes-remoteonly-example
    // Client configuration with RemoteOnly mode
    // No repository dependencies - everything goes over HTTP

    public void ConfigureRemoteOnlyClient(IServiceCollection services, string serverUrl)
    {
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(FactoryModesSamples).Assembly);

        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient
            {
                BaseAddress = new Uri(serverUrl),
                Timeout = TimeSpan.FromSeconds(30)
            });
    }
    #endregion

    #region modes-logical-example
    // Single-tier setup with Logical mode

    public void ConfigureSingleTierApp(IServiceCollection services)
    {
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            typeof(FactoryModesSamples).Assembly);

        // All services available locally
        services.AddSingleton<IPersonRepository, PersonRepository>();
        services.AddSingleton<IOrderRepository, OrderRepository>();
    }

    // Usage - executes directly, no HTTP or serialization
    // var entity = factory.Create();
    // entity.Name = "Local Entity";
    // await factory.Save(entity); // Runs locally
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

        // Local-only method - no [Remote] attribute
        // Executes on client/server directly
        [Fetch]
        public void FetchLocal(string data)
        {
            LocalOnlyData = data;
        }

        // Remote method - serializes and executes on server
        [Remote, Fetch]
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
    // Enable verbose logging to see mode behavior
    //
    // services.AddLogging(builder =>
    // {
    //     builder.AddConsole();
    //     builder.SetMinimumLevel(LogLevel.Debug);
    //     builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
    // });
    //
    // services.AddNeatooRemoteFactory(mode, typeof(MyEntity).Assembly);
    //
    // Logs will show:
    // - "Executing local factory method..." for Server/Logical modes
    // - "Sending remote factory request..." for Remote mode
    // - Serialization format and payload size
    #endregion

    // Supporting types for samples
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
}
