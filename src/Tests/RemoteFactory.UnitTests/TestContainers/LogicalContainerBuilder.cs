using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory;
using System.Reflection;

namespace RemoteFactory.UnitTests.TestContainers;

/// <summary>
/// Fluent builder for creating a Logical mode DI container for unit testing.
/// Logical mode combines client-side factory interfaces with local method execution.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder specifically for tests in the Logical/ namespace that verify:
/// - IFactorySave&lt;T&gt; resolution and behavior
/// - factory.Save() with [Remote] methods executing locally
/// - Behavioral equivalence between Logical and Server modes
/// </para>
/// <para>
/// Logical mode differs from Server mode in how it handles [Remote] attributed methods.
/// In Server mode, [Remote] methods are direct implementations.
/// In Logical mode, [Remote] methods use the client-side factory interface pattern
/// but execute locally without serialization.
/// </para>
/// </remarks>
public sealed class LogicalContainerBuilder
{
    private readonly ServiceCollection _services = new();

    public LogicalContainerBuilder()
    {
        _services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
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
    public LogicalContainerBuilder WithService<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddScoped<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a singleton service implementation for testing.
    /// </summary>
    public LogicalContainerBuilder WithSingleton<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddSingleton<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a transient service implementation for testing.
    /// </summary>
    public LogicalContainerBuilder WithTransient<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddTransient<TInterface, TImpl>();
        return this;
    }

    /// <summary>
    /// Registers a pre-existing instance as a singleton.
    /// </summary>
    public LogicalContainerBuilder WithInstance<TInterface>(TInterface instance)
        where TInterface : class
    {
        _services.AddSingleton(instance);
        return this;
    }

    /// <summary>
    /// Provides access to the underlying service collection for advanced scenarios.
    /// </summary>
    public LogicalContainerBuilder ConfigureServices(Action<IServiceCollection> configure)
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
