using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventRelay;

/// <summary>
/// Integration tests for the factory event relay pattern: server raises events → captured
/// in response → delivered to the consumer's <see cref="IFactoryEventRelay"/> fire-and-forget
/// after the factory method returns.
/// </summary>
public class FactoryEventRelayTests
{
    [Fact]
    public async Task SingleEventRelay_ConsumerReceivesEvent()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();
        var result = await createDel("test");

        await RelayTestHarness.WaitForAsync(
            () => relay.ReceivedOfType<TestRelayEvent>().Count > 0,
            "the relay to receive a TestRelayEvent");

        var received = Assert.Single(relay.ReceivedOfType<TestRelayEvent>());
        Assert.Equal(result.Id, received.Id);
        Assert.Equal("Created: test", received.Message);
    }

    [Fact]
    public async Task MultipleEventsRelay_ArriveInServerRaiseOrder()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.CreateWithMultipleEvents>();
        await createDel("test");

        await RelayTestHarness.WaitForAsync(() => relay.Received.Count >= 2, "both relayed events");

        var batch = relay.Received;
        Assert.Collection(batch,
            e => Assert.IsType<TestRelayEvent>(e),
            e => Assert.IsType<TestRelayEventB>(e));
        Assert.Equal("First", ((TestRelayEvent)batch[0]).Message);
        Assert.Equal(2, ((TestRelayEventB)batch[1]).Sequence);
    }

    [Fact]
    public async Task ServerOnlyEvent_ExcludedFromRelayBatch()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.CreateWithServerOnlyEvent>();
        await createDel("test");

        // One [Remote] call = one Relay invocation, even when the batch is empty. The
        // wait carries this test: an empty Received is also what a relay that never
        // fired looks like, so the timeout has to fail rather than fall through.
        await RelayTestHarness.WaitForAsync(() => relay.InvocationCount == 1, "the single Relay invocation");
        Assert.Empty(relay.Received);
    }

    [Fact]
    public async Task NoEvents_RelayInvokedOnceWithEmptyBatch()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.CreateNoEvents>();
        await createDel("test");

        await RelayTestHarness.WaitForAsync(() => relay.InvocationCount == 1, "the single Relay invocation");
        Assert.Equal(1, relay.InvocationCount);
        Assert.Empty(relay.Received);
    }

    [Fact]
    public async Task RelayException_DoesNotPropagateToFactoryCaller()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();
        relay.OnRelay = _ => throw new InvalidOperationException("Relay failure must be isolated");

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();

        // Caller observes successful return — exception is swallowed inside the fire-and-forget.
        var result = await createDel("test");
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);

        // InvocationCount should still increment even though the handler threw (Relay
        // was called before the exception).
        await RelayTestHarness.WaitForAsync(() => relay.InvocationCount >= 1, "Relay to be invoked despite throwing");
        Assert.Equal(1, relay.InvocationCount);
    }

    [Fact]
    public async Task NoConsumerRegistration_NoOpRelayResolved()
    {
        var (server, client, local) = ClientServerContainers.Scopes();

        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        Assert.Equal("NoOpFactoryEventRelay", relay.GetType().Name);

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();
        var result = await createDel("test");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ServerOnlyCombinedFlags_NotRelayed()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.CreateWithServerOnlyCombinedFlags>();
        await createDel("test");

        await RelayTestHarness.WaitForAsync(() => relay.InvocationCount == 1, "the single Relay invocation");
        Assert.Empty(relay.Received);
    }
}
