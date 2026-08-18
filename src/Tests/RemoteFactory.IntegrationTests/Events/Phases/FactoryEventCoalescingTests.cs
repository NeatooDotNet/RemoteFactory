using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.Phases;

/// <summary>
/// End-to-end coverage for PHASE-006: the attribute's <c>Coalesce</c> named argument,
/// from declaration through the generated registrar to the scheduler's pending-queue
/// collapse — paired with the backcompat control that pins today's one-run-per-raise
/// contract for handlers that don't opt in.
/// </summary>
public class FactoryEventCoalescingTests
{
    private static int RunsFor(IServiceScope scope, string marker, Guid id)
    {
        return scope.GetRequiredService<IEventTestService>()
            .GetRecordedEvents()
            .Count(e => e.EntityId == id && e.EventName == marker);
    }

    // Same shape as FactoryEventRelayTests' harness, including its destructuring order.
    private static (IServiceScope server, IServiceScope client, RecordingFactoryEventRelay relay) ScopesWithRelay()
    {
        var relay = new RecordingFactoryEventRelay();
        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services => services.AddSingleton<Neatoo.RemoteFactory.IFactoryEventRelay>(relay));
        return (server, client, relay);
    }

    private static async Task WaitForAsync(Func<bool> predicate, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
            {
                return;
            }
            await Task.Delay(5);
        }
    }

    /// <summary>
    /// The relay batch is untouched by coalescing (todo AC clause, pinned rather than
    /// inherited from structure): three identical raises reach the client relay as
    /// three events even though the coalescing handler ran once. Collection happens at
    /// Raise, before any queueing — a future move of dedupe earlier (e.g. toward
    /// cross-event coalescing) that starts dropping relayed events turns this red.
    /// </summary>
    [Fact]
    public async Task RemoteExecute_CoalescingHandler_RelayStillReceivesEveryRaise()
    {
        var (server, client, relay) = ScopesWithRelay();
        var run = client.ServiceProvider.GetRequiredService<CoalescingCommands.RunCoalesced>();
        var id = Guid.NewGuid();

        await run(id);

        await WaitForAsync(() => relay.ReceivedOfType<CoalescedRecomputeEvent>().Count >= 3);

        Assert.Equal(3, relay.ReceivedOfType<CoalescedRecomputeEvent>().Count(e => e.Id == id));
        Assert.Equal(1, RunsFor(server, "coalesced-run", id));
    }

    [Fact]
    public async Task RemoteExecute_CoalescingHandler_ThreeIdenticalRaises_RunsOnce()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var run = client.GetRequiredService<CoalescingCommands.RunCoalesced>();
        var id = Guid.NewGuid();

        await run(id);

        Assert.Equal(1, RunsFor(server, "coalesced-run", id));
    }

    [Fact]
    public async Task LogicalExecute_CoalescingHandler_ThreeIdenticalRaises_RunsOnce()
    {
        var (_, _, local) = ClientServerContainers.Scopes();
        var run = local.GetRequiredService<CoalescingCommands.RunCoalesced>();
        var id = Guid.NewGuid();

        await run(id);

        Assert.Equal(1, RunsFor(local, "coalesced-run", id));
    }

    /// <summary>
    /// The backcompat contract, pinned positively: without the flag, N raises are N
    /// dispatches. Distinct handler class and event type from the coalescing case —
    /// the process-global registry's first-wins dedupe would otherwise let one case
    /// silently measure the other's flag.
    /// </summary>
    [Fact]
    public async Task RemoteExecute_NonCoalescingHandler_ThreeIdenticalRaises_RunsThrice()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var run = client.GetRequiredService<CoalescingCommands.RunUncoalesced>();
        var id = Guid.NewGuid();

        await run(id);

        Assert.Equal(3, RunsFor(server, "uncoalesced-run", id));
    }
}
