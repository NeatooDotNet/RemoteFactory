using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// PHASE-003 ENTRY-CALL TARGETS
// =============================================================================
//
// End-to-end targets for entry-call tracking and the framework-owned AfterCommit
// drain. Handlers are registered through FactoryEventHandlerRegistry's 3-arg
// overload (see PhaseHandlerRegistrations) rather than a [FactoryEventHandler<T>]
// phase argument — the generator does not thread the attribute's phase until
// PHASE-002 lands.
//
// One event type per scenario: the registry is process-global with
// (eventType, handlerClass) first-registration-wins dedupe, so sharing an event
// type across scenarios silently drops registrations.

// -----------------------------------------------------------------------------
// EVENTS
// -----------------------------------------------------------------------------

public record PhasedCreateEvent(Guid Id) : FactoryEventBase;
public record PhasedStaticCommandEvent(Guid Id) : FactoryEventBase;
public record PhasedSaveEvent(Guid Id) : FactoryEventBase;
public record PhasedSweepEvent(Guid Id) : FactoryEventBase;
public record PhasedFailureEvent(Guid Id) : FactoryEventBase;
public record PhasedForbiddenEvent(Guid Id) : FactoryEventBase;
public record PhasedThrowingHandlerEvent(Guid Id) : FactoryEventBase;
public record PhasedRelayChainEvent(Guid Id) : FactoryEventBase;
public record PhasedRelayOutEvent(Guid Id) : FactoryEventBase;
public record PhasedUntypedRemoteEvent(Guid Id) : FactoryEventBase;
public record PhasedCancelAfterSuccessEvent(Guid Id) : FactoryEventBase;

// -----------------------------------------------------------------------------
// HANDLER MARKER CLASSES — registration keys only; the invokers are lambdas in
// PhaseHandlerRegistrations
// -----------------------------------------------------------------------------

public sealed class PhasedImmediateMarker { }
public sealed class PhasedAfterCommitMarker { }
public sealed class PhasedAfterFlushMarker { }
public sealed class PhasedThrowerMarker { }
public sealed class PhasedSurvivorMarker { }
public sealed class PhasedChainRaiserMarker { }

/// <summary>
/// Registers the phased handlers exactly once per process (the registry's
/// first-registration-wins dedupe makes repeated calls no-ops).
/// </summary>
public static class PhaseHandlerRegistrations
{
    private static readonly Lazy<bool> Registered = new(RegisterAll);

    public static void EnsureRegistered() => _ = Registered.Value;

    private static bool RegisterAll()
    {
        FactoryEventHandlerRegistry.RegisterHandler<PhasedCreateEvent>(
            typeof(PhasedImmediateMarker), DispatchPhase.Immediate,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("immediate", ((PhasedCreateEvent)evt).Id);
                return Task.CompletedTask;
            });
        FactoryEventHandlerRegistry.RegisterHandler<PhasedCreateEvent>(
            typeof(PhasedAfterCommitMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("after-commit", ((PhasedCreateEvent)evt).Id);
                return Task.CompletedTask;
            });

        FactoryEventHandlerRegistry.RegisterHandler<PhasedStaticCommandEvent>(
            typeof(PhasedAfterCommitMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("static-after-commit", ((PhasedStaticCommandEvent)evt).Id);
                return Task.CompletedTask;
            });

        FactoryEventHandlerRegistry.RegisterHandler<PhasedSaveEvent>(
            typeof(PhasedAfterCommitMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("save-after-commit", ((PhasedSaveEvent)evt).Id);
                return Task.CompletedTask;
            });

        // Registration order deliberately inverts drain order: AfterCommit first,
        // AfterFlush second. The sweep drains AfterFlush first regardless.
        FactoryEventHandlerRegistry.RegisterHandler<PhasedSweepEvent>(
            typeof(PhasedAfterCommitMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("sweep-commit", ((PhasedSweepEvent)evt).Id);
                return Task.CompletedTask;
            });
        FactoryEventHandlerRegistry.RegisterHandler<PhasedSweepEvent>(
            typeof(PhasedAfterFlushMarker), DispatchPhase.AfterFlush,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("sweep-flush", ((PhasedSweepEvent)evt).Id);
                return Task.CompletedTask;
            });

        FactoryEventHandlerRegistry.RegisterHandler<PhasedFailureEvent>(
            typeof(PhasedAfterCommitMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("failure-after-commit", ((PhasedFailureEvent)evt).Id);
                return Task.CompletedTask;
            });

        FactoryEventHandlerRegistry.RegisterHandler<PhasedForbiddenEvent>(
            typeof(PhasedAfterCommitMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("forbidden-after-commit", ((PhasedForbiddenEvent)evt).Id);
                return Task.CompletedTask;
            });

        // Thrower first, survivor second: the post-completion drain must swallow the
        // throw and still run the survivor.
        FactoryEventHandlerRegistry.RegisterHandler<PhasedThrowingHandlerEvent>(
            typeof(PhasedThrowerMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("thrower", ((PhasedThrowingHandlerEvent)evt).Id);
                throw new InvalidOperationException("AfterCommit handler throws (swallow expected)");
            });
        FactoryEventHandlerRegistry.RegisterHandler<PhasedThrowingHandlerEvent>(
            typeof(PhasedSurvivorMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("survivor", ((PhasedThrowingHandlerEvent)evt).Id);
                return Task.CompletedTask;
            });

        // Raises a second event from inside the AfterCommit drain. The raise happens
        // while the entry is still active and before relay collection, so the new
        // event must join the same response's relay batch.
        FactoryEventHandlerRegistry.RegisterHandler<PhasedRelayChainEvent>(
            typeof(PhasedChainRaiserMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, ct) =>
                sp.GetRequiredService<IFactoryEvents>().Raise(new PhasedRelayOutEvent(((PhasedRelayChainEvent)evt).Id), RaiseOptions.None, ct));

        FactoryEventHandlerRegistry.RegisterHandler<PhasedUntypedRemoteEvent>(
            typeof(PhasedAfterCommitMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("untyped-after-commit", ((PhasedUntypedRemoteEvent)evt).Id);
                return Task.CompletedTask;
            });

        FactoryEventHandlerRegistry.RegisterHandler<PhasedCancelAfterSuccessEvent>(
            typeof(PhasedAfterCommitMarker), DispatchPhase.AfterCommit,
            (sp, evt, _, _) =>
            {
                sp.GetRequiredService<IEventTestService>().RecordEventFired("cancel-after-commit", ((PhasedCancelAfterSuccessEvent)evt).Id);
                return Task.CompletedTask;
            });

        return true;
    }
}

// -----------------------------------------------------------------------------
// FACTORY TARGETS
// -----------------------------------------------------------------------------

/// <summary>
/// Entity whose factory methods raise phased events. Create covers the class
/// renderer's read seam; Insert (reached through Save's LocalSave nesting) covers
/// the write seam and the drains-exactly-once-at-the-outermost invariant.
/// </summary>
[Factory]
public partial class PhaseEntryTarget : IFactorySaveMeta
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    [Create]
    [Remote]
    internal async Task Create(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] IEventTestService testService,
        CancellationToken ct)
    {
        Id = id;
        await events.Raise(new PhasedCreateEvent(id), RaiseOptions.None, ct);
        testService.RecordEventFired("create-method-done", id);
    }

    [Fetch]
    [Remote]
    internal async Task FetchAndFail(
        Guid id,
        [Service] IFactoryEvents events,
        CancellationToken ct)
    {
        await events.Raise(new PhasedFailureEvent(id), RaiseOptions.None, ct);
        throw new InvalidOperationException($"entry call fails for {id}");
    }

    [Insert]
    [Remote]
    internal async Task Insert(
        [Service] IFactoryEvents events,
        [Service] IEventTestService testService,
        CancellationToken ct)
    {
        await events.Raise(new PhasedSaveEvent(Id), RaiseOptions.None, ct);
        testService.RecordEventFired("insert-method-done", Id);
        IsNew = false;
    }

    [Update]
    [Remote]
    internal Task Update()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Static factory commands — the static renderer's DI-lambda entry seam.
/// </summary>
[Factory]
public static partial class PhaseStaticCommands
{
    [Execute]
    [Remote]
    internal static async Task<Guid> _RunPhased(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] IEventTestService testService,
        CancellationToken ct)
    {
        await events.Raise(new PhasedStaticCommandEvent(id), RaiseOptions.None, ct);
        testService.RecordEventFired("static-method-done", id);
        return id;
    }

    [Execute]
    [Remote]
    internal static async Task<Guid> _RunSweep(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] IEventTestService testService,
        CancellationToken ct)
    {
        await events.Raise(new PhasedSweepEvent(id), RaiseOptions.None, ct);
        testService.RecordEventFired("sweep-method-done", id);
        return id;
    }

    [Execute]
    [Remote]
    internal static async Task<Guid> _RunWithThrowingHandler(
        Guid id,
        [Service] IFactoryEvents events,
        CancellationToken ct)
    {
        await events.Raise(new PhasedThrowingHandlerEvent(id), RaiseOptions.None, ct);
        return id;
    }

    [Execute]
    [Remote]
    internal static async Task<Guid> _RunRelayChain(
        Guid id,
        [Service] IFactoryEvents events,
        CancellationToken ct)
    {
        await events.Raise(new PhasedRelayChainEvent(id), RaiseOptions.None, ct);
        return id;
    }

    /// <summary>
    /// Enqueues phased work, then calls an authorization-denied interface factory —
    /// the falsifiable forbidden-does-not-drain shape (a bare forbidden call has an
    /// empty queue and proves nothing). The interface-factory leg is the harness's
    /// auth shape that THROWS on denial (class-factory reads return
    /// <c>Authorized&lt;T&gt;</c> instead — a successful call; see the plan's Intent).
    /// </summary>
    [Execute]
    [Remote]
    internal static async Task<Guid> _RunForbiddenInner(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] IPhaseDeniedServiceFactory deniedFactory,
        CancellationToken ct)
    {
        await events.Raise(new PhasedForbiddenEvent(id), RaiseOptions.None, ct);
        await deniedFactory.GetSecret();
        return id;
    }

    /// <summary>
    /// Enqueues phased work, then cancels the request token — the call itself has
    /// already succeeded, so the entry drain must still run in full.
    /// </summary>
    [Execute]
    [Remote]
    internal static async Task<Guid> _RunAndCancel(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] PhaseCancellationTrigger trigger,
        CancellationToken ct)
    {
        await events.Raise(new PhasedCancelAfterSuccessEvent(id), RaiseOptions.None, ct);
        trigger.Cancel();
        return id;
    }
}

/// <summary>
/// Test hook that lets a server-side factory method cancel the request token the
/// client passed in — simulating a token that goes cancelled between the entry
/// call succeeding and the response being produced.
/// </summary>
public sealed class PhaseCancellationTrigger
{
    public Action? OnCancel { get; set; }
    public void Cancel() => OnCancel?.Invoke();
}

/// <summary>Authorization that always denies reads.</summary>
public class PhaseDenyAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanRead() => false;
}

/// <summary>
/// Interface factory whose methods are always authorization-denied — the harness's
/// throwing denial shape (NotAuthorizedException from the generated auth check).
/// </summary>
[Factory]
[AuthorizeFactory<PhaseDenyAuth>]
public interface IPhaseDeniedService
{
    Task<string> GetSecret();
}

/// <summary>Named to match IPhaseDeniedService for RegisterMatchingName.</summary>
public class PhaseDeniedService : IPhaseDeniedService
{
    public Task<string> GetSecret() => Task.FromResult("secret");
}
