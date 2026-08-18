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
