using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using System.Reflection;

namespace RemoteFactory.IntegrationTests.TestContainers;

/// <summary>
/// Fluent builder for creating a lightweight DI container for local mode unit testing.
/// Both Logical and Server modes execute methods locally without serialization.
/// </summary>
/// <remarks>
/// Use this builder for:
/// - Server mode tests (NeatooFactory.Server) - ASP.NET Core server scenarios
/// - Logical mode tests (NeatooFactory.Logical) - single-tier app/unit test scenarios
/// - Tests that don't require serialization validation
/// - Faster test execution without remote overhead
///
/// Both modes behave identically - direct local execution without serialization.
/// For tests requiring serialization round-trips, use <see cref="ClientServerContainers"/>.
/// </remarks>
public sealed class LocalContainerBuilder
{
    private readonly ServiceCollection _services = new();
    private readonly NeatooFactory _mode;

    /// <summary>
    /// Creates a new LocalContainerBuilder with the specified factory mode.
    /// </summary>
    /// <param name="mode">The factory mode to use. Defaults to Server mode.</param>
    public LocalContainerBuilder(NeatooFactory mode = NeatooFactory.Server)
    {
        _mode = mode;

        // Configure RemoteFactory
        _services.AddNeatooRemoteFactory(
            mode,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            Assembly.GetExecutingAssembly()
        );

        // Add logging
        _services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        // Add IHostApplicationLifetime
        _services.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();

        // Register all [Factory] decorated types
        RegisterFactoryTypes();
    }

    /// <summary>
    /// Registers a scoped service implementation for testing.
    /// </summary>
    public LocalContainerBuilder WithService<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddScoped<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a singleton service implementation for testing.
    /// </summary>
    public LocalContainerBuilder WithSingleton<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddSingleton<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a transient service implementation for testing.
    /// </summary>
    public LocalContainerBuilder WithTransient<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddTransient<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a pre-existing instance as a singleton.
    /// </summary>
    public LocalContainerBuilder WithInstance<TInterface>(TInterface instance)
        where TInterface : class
    {
        _services.AddSingleton(instance);
        return this;
    }

    /// <summary>
    /// Provides access to the underlying service collection for advanced scenarios.
    /// </summary>
    public LocalContainerBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    /// <summary>
    /// Adds the standard test services (IService, IService2, IService3).
    /// </summary>
    public LocalContainerBuilder WithStandardServices()
    {
        _services.AddScoped<IService, Service>();
        _services.AddScoped<IService2, Service2>();
        _services.AddScoped<IService3, Service3>();
        return this;
    }

    /// <summary>
    /// Builds and returns the service provider.
    /// </summary>
    public IServiceProvider Build() => _services.BuildServiceProvider();

    /// <summary>
    /// Builds the service provider and creates a scoped service provider.
    /// Returns both the root provider (for disposal) and a scope.
    /// </summary>
    public (ServiceProvider provider, IServiceScope scope) BuildWithScope()
    {
        var provider = _services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return (provider, scope);
    }

    /// <summary>
    /// Registers all types decorated with [Factory] attribute as scoped services.
    /// </summary>
    /// <remarks>
    /// Infrastructure reflection for DI registration is acceptable per project guidelines.
    /// </remarks>
    private void RegisterFactoryTypes()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            // Skip generic types and abstract types
            if (type.GenericTypeArguments.Length > 0 || type.IsAbstract)
                continue;

            // Check for [Factory] attribute
            if (type.GetCustomAttribute<FactoryAttribute>() == null)
                continue;

            // Skip records (they have a compiler-generated <Clone>$ method)
            if (type.GetMethod("<Clone>$") != null)
                continue;

            _services.AddScoped(type);
        }
    }
}
