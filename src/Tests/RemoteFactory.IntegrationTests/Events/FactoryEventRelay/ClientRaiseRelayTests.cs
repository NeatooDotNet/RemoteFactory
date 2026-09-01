using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventRelay;

/// <summary>
/// Integration tests for the client-initiated raise → relay path: the client calls
/// <see cref="IFactoryEvents.Raise{T}"/>, the server dispatches handlers and collects,
/// and the batch comes back to the consumer's <see cref="IFactoryEventRelay"/>.
/// </summary>
/// <remarks>
/// <para>
/// Sibling to <c>FactoryEventRelayTests</c>, which covers the same delivery machinery
/// reached through a <c>[Remote]</c> factory method. The server code is shared —
/// <c>HandleRemoteDelegateRequest</c> attaches the collector's contents without looking
/// at which delegate ran — so what these tests actually pin is the CLIENT half, which
/// used to exist only on the factory path.
/// </para>
/// <para>
/// <b>The gap these close.</b> <c>MakeRemoteDelegateRequest.ForDelegateEvent</c> awaited
/// the round-trip and discarded the <c>RemoteResponseDto</c> — no <c>var result =</c>,
/// no reference to the relay field. The server built the batch, serialized it, and the
/// client dropped it. Silently: no exception, no log, no warning.
/// </para>
/// </remarks>
public class ClientRaiseRelayTests
{
    /// <summary>
    /// A client raise with no handler anywhere still relays. Capture happens before
    /// handler lookup, so the batch does not depend on anyone listening.
    /// </summary>
    [Fact]
    public async Task ClientRaise_NoHandlers_RelaysOriginatingEvent()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();
        var id = Guid.NewGuid();

        var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();
        await events.Raise(new ClientRaiseSoloEvent(id));

        await RelayTestHarness.WaitForAsync(
            () => relay.ReceivedOfType<ClientRaiseSoloEvent>().Any(e => e.Id == id),
            "the relay to receive the client's own ClientRaiseSoloEvent");

        var received = Assert.Single(relay.ReceivedOfType<ClientRaiseSoloEvent>(), e => e.Id == id);
        Assert.Equal(id, received.Id);
    }

    /// <summary>
    /// The event a server handler raises reaches the client alongside the client's own,
    /// in server raise order. This is the case the gap swallowed entirely.
    /// </summary>
    [Fact]
    public async Task ClientRaise_HandlerRaisedEvent_RelaysBothInOrder()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();
        var id = Guid.NewGuid();

        var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();
        await events.Raise(new ClientRaiseChainEvent(id));

        await RelayTestHarness.WaitForAsync(
            () => relay.ReceivedOfType<ClientRaiseChainedEvent>().Any(e => e.Id == id),
            "the relay to receive the handler-raised ClientRaiseChainedEvent");

        // Filter to this test's id: the relay instance is per-scope, but asserting on
        // identity rather than position keeps the ordering claim honest if the suite
        // ever runs another raise through the same relay.
        var mine = relay.Received
            .Where(e => e is ClientRaiseChainEvent c && c.Id == id
                     || e is ClientRaiseChainedEvent h && h.Id == id)
            .ToList();

        Assert.Collection(mine,
            e => Assert.IsType<ClientRaiseChainEvent>(e),
            e => Assert.IsType<ClientRaiseChainedEvent>(e));
    }

    /// <summary>
    /// <see cref="RaiseOptions.ServerOnly"/> means the same thing from the client as it
    /// does server-side: handlers run, nothing relays.
    /// </summary>
    /// <remarks>
    /// The wait carries this test. An empty <c>Received</c> is also exactly what a relay
    /// that never fired looks like — which is what the broken build produced — so the
    /// invocation-count wait has to fail as a timeout rather than fall through into a
    /// vacuously-passing emptiness assertion.
    /// </remarks>
    [Fact]
    public async Task ClientRaise_ServerOnly_RelayInvokedWithEmptyBatch()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();
        var id = Guid.NewGuid();

        var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();
        await events.Raise(new ClientRaiseSuppressedEvent(id), RaiseOptions.ServerOnly);

        await RelayTestHarness.WaitForAsync(
            () => relay.InvocationCount == 1,
            "the single Relay invocation for a ServerOnly client raise");

        Assert.Empty(relay.Received);
    }

    /// <summary>
    /// An event raised by an <c>AfterCommit</c> handler joins the same response's batch.
    /// The framework drain runs before the collector read, so the client-raise path gets
    /// the same guarantee the factory path documents.
    /// </summary>
    [Fact]
    public async Task ClientRaise_AfterCommitHandlerRaises_JoinsSameRelayBatch()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();
        var id = Guid.NewGuid();

        var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();
        await events.Raise(new ClientRaiseDeferredEvent(id));

        await RelayTestHarness.WaitForAsync(
            () => relay.ReceivedOfType<ClientRaiseDeferredChainedEvent>().Any(e => e.Id == id),
            "the relay to receive the AfterCommit-raised ClientRaiseDeferredChainedEvent");

        Assert.Single(relay.ReceivedOfType<ClientRaiseDeferredEvent>(), e => e.Id == id);
        Assert.Single(relay.ReceivedOfType<ClientRaiseDeferredChainedEvent>(), e => e.Id == id);
    }

    /// <summary>
    /// A throwing server handler faults the client's <c>Raise</c> and nothing relays —
    /// the response never comes back, so there is no batch to deliver.
    /// </summary>
    /// <remarks>
    /// Companion to <c>FactoryEventHandlerSerializationTests.ClientRaise_ServerHandlerThrows_ExceptionSurfacesToClient</c>,
    /// which pins the exception. This one pins that the failure path stays silent on the
    /// relay rather than delivering a partial batch.
    /// </remarks>
    [Fact]
    public async Task ClientRaise_ServerHandlerThrows_NothingRelayed()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();
        var id = Guid.NewGuid();

        var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();

        await Assert.ThrowsAnyAsync<Exception>(
            () => events.Raise(new ClientRaiseThrowingEvent(id)));

        Assert.Empty(relay.ReceivedOfType<ClientRaiseThrowingEvent>());
        Assert.Equal(0, relay.InvocationCount);
    }
}
