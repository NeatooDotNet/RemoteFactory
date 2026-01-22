using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory;
using System.Reflection;

namespace RemoteFactory.UnitTests.TestContainers;

/// <summary>
/// Fluent builder for creating a Server mode DI container for unit testing.
/// Server mode executes factory methods locally without serialization round-trips.
/// </summary>
/// <remarks>
/// Use this builder for most unit tests. For Logical mode tests, use <see cref="LogicalContainerBuilder"/>.
/// For integration tests requiring client/server serialization, see RemoteFactory.IntegrationTests.
/// </remarks>
public sealed class ServerContainerBuilder
{
    private readonly ServiceCollection _services = new();

    public ServerContainerBuilder()
    {
        _services.AddNeatooRemoteFactory(
            NeatooFactory.Server,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            Assembly.GetExecutingAssembly()
        );
        _services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        // Register all [Factory] decorated types
        RegisterFactoryTypes();
    }

    /// <summary>
    /// Registers a service implementation for testing.
    /// </summary>
    public ServerContainerBuilder WithService<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddScoped<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a singleton service implementation for testing.
    /// </summary>
    public ServerContainerBuilder WithSingleton<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddSingleton<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a transient service implementation for testing.
    /// </summary>
    public ServerContainerBuilder WithTransient<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddTransient<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a pre-existing instance as a singleton.
    /// </summary>
    public ServerContainerBuilder WithInstance<TInterface>(TInterface instance)
        where TInterface : class
    {
        _services.AddSingleton(instance);
        return this;
    }

    /// <summary>
    /// Provides access to the underlying service collection for advanced scenarios.
    /// </summary>
    public ServerContainerBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
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
    /// This mirrors the behavior of ClientServerContainers.RegisterIfAttribute().
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
