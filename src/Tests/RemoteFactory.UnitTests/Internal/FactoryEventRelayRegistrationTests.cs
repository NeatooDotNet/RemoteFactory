using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Verifies DI registration behavior for IFactoryEventRelay across NeatooFactory modes
/// and against consumer overrides.
/// </summary>
public class FactoryEventRelayRegistrationTests
{
    private sealed class FakeRelay : IFactoryEventRelay
    {
        public Task Relay(IReadOnlyList<FactoryEventBase> events) => Task.CompletedTask;
    }

    [Fact]
    public void RemoteMode_NoConsumerRelay_ResolvesNoOpDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(FactoryEventRelayRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var relay = provider.GetRequiredService<IFactoryEventRelay>();

        Assert.Equal("NoOpFactoryEventRelay", relay.GetType().Name);
    }

    [Fact]
    public void ServerMode_IFactoryEventRelay_NotRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventRelayRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<IFactoryEventRelay>());
    }

    [Fact]
    public void LogicalMode_IFactoryEventRelay_NotRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(FactoryEventRelayRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<IFactoryEventRelay>());
    }

    [Fact]
    public void RemoteMode_ConsumerRegistersBeforeAdd_TryAddKeepsConsumerRegistration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Consumer-first registration — TryAdd must NOT replace it with NoOp.
        services.AddSingleton<IFactoryEventRelay, FakeRelay>();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(FactoryEventRelayRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<FakeRelay>(provider.GetRequiredService<IFactoryEventRelay>());
    }

    [Fact]
    public void RemoteMode_ConsumerRegistersAfterAdd_OverridesNoOp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(FactoryEventRelayRegistrationTests).Assembly);
        // Consumer registration after — standard DI last-writer-wins replaces the NoOp.
        services.AddSingleton<IFactoryEventRelay, FakeRelay>();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<FakeRelay>(provider.GetRequiredService<IFactoryEventRelay>());
    }
}
