using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.Phases;

/// <summary>
/// End-to-end coverage for PHASE-004: the consumer-facing AfterFlush drain point
/// (<c>IFactoryEventPhaseCoordinator</c>) and its fail-open warning.
/// </summary>
/// <remarks>
/// Same falsifiability condition as the PHASE-002 attribute tests: nothing here — and
/// nothing in <c>FactoryEventPhaseCoordinatorTargets</c> — calls
/// <c>FactoryEventHandlerRegistry.RegisterHandler</c>; registration is the generated
/// registrar's work. On top of that, every sequence assertion leans on the
/// "<c>*-method-done</c>" marker: an AfterFlush marker recorded BEFORE it proves the
/// consumer's drain ran it, because the only other path that runs AfterFlush work —
/// the PHASE-002 fail-open sweep — fires after the method body returns.
/// </remarks>
public class FactoryEventPhaseCoordinatorTests
{
    private static List<string> RecordedFor(IServiceScope scope, Guid id)
    {
        return scope.GetRequiredService<IEventTestService>()
            .GetRecordedEvents()
            .Where(e => e.EntityId == id)
            .Select(e => e.EventName)
            .ToList();
    }

    /// <summary>
    /// HTTP-dispatched <c>[Remote]</c> entry call: the attribute-declared AfterFlush
    /// handler runs at the consumer's coordinator call, inside the method body.
    /// </summary>
    [Fact]
    public async Task RemoteCreate_CoordinatorDrain_RunsAfterFlushHandlersAtTheConsumersPoint()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ICoordinatorDrainTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Create(id);

        // A no-op coordinator produces ["coord-method-done", "coord-flush"] — the
        // fail-open sweep runs the handler only after the method body returns.
        Assert.Equal(["coord-flush", "coord-method-done"], RecordedFor(server, id));
    }

    /// <summary>
    /// Direct Logical invocation reaches the same drain point through the generated
    /// wrapper seam.
    /// </summary>
    [Fact]
    public async Task LogicalCreate_CoordinatorDrain_RunsAfterFlushHandlersAtTheConsumersPoint()
    {
        var (_, _, local) = ClientServerContainers.Scopes();
        var factory = local.GetRequiredService<ICoordinatorDrainTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Create(id);

        Assert.Equal(["coord-flush", "coord-method-done"], RecordedFor(local, id));
    }

    /// <summary>
    /// The todo's AC-1 ordering guarantee as one observed sequence, with the consumer
    /// drain in play: Immediate at raise, AfterFlush at the coordinator call,
    /// AfterCommit after completion — against a raise order chosen to be the reverse.
    /// The second "ord-immediate" (raised after the drain) pins the scoped ordering
    /// sentence from plan review A-C3: later Immediate work interleaves between drain
    /// points rather than being barriered before all AfterFlush work.
    /// </summary>
    [Fact]
    public async Task RemoteExecute_ThreePhases_RunInPhaseOrderNotRaiseOrder()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var run = client.GetRequiredService<CoordinatorOrderingCommands.RunOrdered>();
        var id = Guid.NewGuid();

        await run(id);

        Assert.Equal(
            ["ord-immediate", "ord-flush", "ord-immediate", "ord-method-done", "ord-commit"],
            RecordedFor(server, id));
    }

    /// <summary>Same sequence through the Logical container.</summary>
    [Fact]
    public async Task LogicalExecute_ThreePhases_RunInPhaseOrderNotRaiseOrder()
    {
        var (_, _, local) = ClientServerContainers.Scopes();
        var run = local.GetRequiredService<CoordinatorOrderingCommands.RunOrdered>();
        var id = Guid.NewGuid();

        await run(id);

        Assert.Equal(
            ["ord-immediate", "ord-flush", "ord-immediate", "ord-method-done", "ord-commit"],
            RecordedFor(local, id));
    }

    /// <summary>
    /// Fail-open end to end: an attribute-declared AfterFlush handler whose consumer
    /// never drains still runs — after the method body, at the entry sweep — and the
    /// dedicated 9007 warning names the event type in the server's logs. The warning is
    /// the load-bearing half: the sweep itself ships since PHASE-002.
    /// </summary>
    [Fact]
    public async Task RemoteCreate_NeverDrainedAfterFlush_RunsAtTheSweepWithTheWarning()
    {
        var (server, client, _) = ClientServerContainers.ScopesWithLogging(out var logger);
        var factory = client.GetRequiredService<ICoordinatorFailOpenTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Create(id);

        Assert.Equal(["failopen-method-done", "failopen-flush"], RecordedFor(server, id));

        Assert.Contains(logger.Messages, m =>
            m.EventId.Id == 9007 && m.Message.Contains(nameof(CoordinatorFailOpenEvent)));
    }

    /// <summary>
    /// The drained path is warning-free: the consumer who calls the coordinator gets no
    /// 9007 — the warning discriminates, it does not fire on every AfterFlush handler.
    /// </summary>
    [Fact]
    public async Task RemoteCreate_ConsumerDrainedAfterFlush_ProducesNoWarning()
    {
        var (server, client, _) = ClientServerContainers.ScopesWithLogging(out var logger);
        var factory = client.GetRequiredService<ICoordinatorDrainTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Create(id);

        Assert.Equal(["coord-flush", "coord-method-done"], RecordedFor(server, id));
        Assert.DoesNotContain(logger.Messages, m => m.EventId.Id == 9007);
    }
}
