using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Covers the entry-call wrapper generated factory code routes through (PHASE-003):
/// depth-aware begin/end, drain at the outermost successful completion only, clear on
/// failure, the no-token drain policy, and null-tolerance when no scheduler exists.
/// </summary>
public class FactoryEntryCallTests
{
    // One event type per test: FactoryEventHandlerRegistry is process-global with
    // (eventType, handlerClass) first-registration-wins dedupe, so sharing an event
    // type across tests silently drops the later registration (PHASE-007 tech debt).
    private sealed record EntryEvent(string Value) : FactoryEventBase;
    private sealed record FailingEntryEvent(string Value) : FactoryEventBase;
    private sealed record NestedEntryEvent(string Value) : FactoryEventBase;
    private sealed record SyncEntryEvent(string Value) : FactoryEventBase;
    private sealed record TokenCaptureEvent(string Value) : FactoryEventBase;

    private sealed class DeferredHandler { }

    private static (ServiceProvider Provider, IServiceScope Scope) ServerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEntryCallTests).Assembly);
        var provider = services.BuildServiceProvider();
        return (provider, provider.CreateScope());
    }

    [Fact]
    public async Task RunAsync_DeferredWorkDrainsAtCompletion_AndResultFlowsThrough()
    {
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<EntryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("deferred"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();

            var result = await FactoryEntryCall.RunAsync(sp, async () =>
            {
                await events.Raise(new EntryEvent("x"));
                Assert.Empty(dispatched);
                return 42;
            });

            Assert.Equal(42, result);
            Assert.Equal(["deferred"], dispatched);
        }
    }

    [Fact]
    public async Task RunAsync_NestedEntry_DoesNotDrainAtTheInnerCompletion()
    {
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<NestedEntryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("deferred"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();

            await FactoryEntryCall.RunAsync(sp, async () =>
            {
                await FactoryEntryCall.RunAsync(sp, async () =>
                {
                    await events.Raise(new NestedEntryEvent("x"));
                    return 0;
                });

                // The inner entry completed successfully — but it is nested, so the
                // deferred work must still be waiting for the OUTERMOST completion.
                Assert.Empty(dispatched);
                return 0;
            });

            Assert.Equal(["deferred"], dispatched);
        }
    }

    [Fact]
    public async Task RunAsync_BodyThrows_DeferredWorkIsClearedNotRun()
    {
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<FailingEntryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("deferred"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();
            var scheduler = sp.GetRequiredService<IFactoryEventPhaseScheduler>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                FactoryEntryCall.RunAsync<int>(sp, async () =>
                {
                    await events.Raise(new FailingEntryEvent("x"));
                    throw new InvalidOperationException("entry failed");
                }));

            Assert.Empty(dispatched);
            Assert.False(scheduler.HasPending);
        }
    }

    [Fact]
    public void Run_SyncEntryWithDeferredWork_DoesNotLoseIt()
    {
        // A synchronous (non-Task) factory method can still enqueue phased work via a
        // fire-and-forget Raise. The sync wrapper block-drains at completion rather than
        // silently dropping it — the no-silent-loss invariant.
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<SyncEntryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("deferred"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();

            var result = FactoryEntryCall.Run(sp, () =>
            {
                events.Raise(new SyncEntryEvent("x")).GetAwaiter().GetResult();
                return "done";
            });

            Assert.Equal("done", result);
            Assert.Equal(["deferred"], dispatched);
        }
    }

    [Fact]
    public async Task RunAsync_EntryDrainPassesNoCancellationToken()
    {
        // B-C5 policy: the entry call already succeeded, so nothing may abort its
        // post-completion work — drained handlers receive CancellationToken.None even
        // when the factory call itself carried a live token.
        CancellationToken? received = null;
        FactoryEventHandlerRegistry.RegisterHandler<TokenCaptureEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, ct) => { received = ct; return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();
            using var cts = new CancellationTokenSource();

            await FactoryEntryCall.RunAsync(sp, async () =>
            {
                await events.Raise(new TokenCaptureEvent("x"), RaiseOptions.None, cts.Token);
                return 0;
            });

            Assert.Equal(CancellationToken.None, received);
        }
    }

    [Fact]
    public async Task RunAsync_NoSchedulerInScope_JustRunsTheBody()
    {
        // Remote-mode (client) containers register no scheduler; client-reachable
        // generated wrappers must reduce to the body.
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        var result = await FactoryEntryCall.RunAsync(provider, () => Task.FromResult(7));

        Assert.Equal(7, result);
    }

    [Fact]
    public async Task EndEntryCall_WithoutBegin_ThrowsOnTheSuccessPath()
    {
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<MisuseRecoveryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("deferred"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var scheduler = sp.GetRequiredService<IFactoryEventPhaseScheduler>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.EndEntryCallAsync(success: true));

            // The failure path runs inside catch blocks and must never throw — and it
            // must not corrupt depth (no negative depth, no phantom entry).
            await scheduler.EndEntryCallAsync(success: false);
            Assert.False(scheduler.IsEntryCallActive);

            // A subsequent normal entry cycle still works.
            var events = sp.GetRequiredService<IFactoryEvents>();
            await FactoryEntryCall.RunAsync(sp, async () =>
            {
                await events.Raise(new MisuseRecoveryEvent("x"));
                return 0;
            });
            Assert.Equal(["deferred"], dispatched);
        }
    }

    [Fact]
    public async Task HandlerThrowsOperationCanceled_MidDrain_EntryExitStillClearsAndDepthSurvives()
    {
        // The one exit path where EndEntryCallAsync(true) itself throws: a handler's own
        // OperationCanceledException aborts the drain (scheduler contract), the helper's
        // catch then calls EndEntryCallAsync(false) — a second End for one Begin, safe by
        // the depth tolerance — and "between entry calls the scheduler is empty" must
        // still hold on the way out.
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<OceThrowingEvent>(typeof(PhaseOceThrower), DispatchPhase.AfterCommit,
            (_, _, _, _) => throw new OperationCanceledException());
        FactoryEventHandlerRegistry.RegisterHandler<OceThrowingEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("behind-the-oce"); return Task.CompletedTask; });
        FactoryEventHandlerRegistry.RegisterHandler<OceRecoveryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("recovery"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();
            var scheduler = sp.GetRequiredService<IFactoryEventPhaseScheduler>();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                FactoryEntryCall.RunAsync(sp, async () =>
                {
                    await events.Raise(new OceThrowingEvent("x"));
                    return 0;
                }));

            // The aborted drain's leftover was cleared (a clear, never a drain) and the
            // entry state fully released.
            Assert.Empty(dispatched);
            Assert.False(scheduler.HasPending);
            Assert.False(scheduler.IsEntryCallActive);

            // The scope is reusable: a fresh entry drains only its own work.
            await FactoryEntryCall.RunAsync(sp, async () =>
            {
                await events.Raise(new OceRecoveryEvent("y"));
                return 0;
            });
            Assert.Equal(["recovery"], dispatched);
        }
    }

    [Fact]
    public async Task NestedEntryFails_OuterCatchesAndSucceeds_TheEntryStillDrains()
    {
        // A nested factory call failing is not the entry failing: only the OUTERMOST
        // exit decides drain-vs-clear. When the outer body catches the inner failure and
        // completes, the entry succeeded — everything still queued (including what the
        // failed inner section enqueued before throwing) drains with it. Pinned so an
        // "always clear on any failure" refactor goes red here.
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<CaughtInnerEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("from-inner"); return Task.CompletedTask; });
        FactoryEventHandlerRegistry.RegisterHandler<CaughtOuterEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("from-outer"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();

            await FactoryEntryCall.RunAsync(sp, async () =>
            {
                try
                {
                    await FactoryEntryCall.RunAsync<int>(sp, async () =>
                    {
                        await events.Raise(new CaughtInnerEvent("x"));
                        throw new InvalidOperationException("inner fails");
                    });
                }
                catch (InvalidOperationException)
                {
                    // The outer entry chooses to continue.
                }

                await events.Raise(new CaughtOuterEvent("y"));
                return 0;
            });

            Assert.Equal(["from-inner", "from-outer"], dispatched);
        }
    }

    [Fact]
    public async Task DrainedHandlerInvokingAFactory_NestsWithoutDrainingOrClearingTheDrainInProgress()
    {
        // Realistic projection-handler shape: an AfterCommit handler writes through a
        // factory. That factory call is a NESTED entry (the entry is still active during
        // the drain), so it must neither drain mid-drain nor throw — and phased work it
        // raises joins the drain in progress.
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<HandlerFactoryCallEvent>(typeof(PhaseFactoryCallingHandler), DispatchPhase.AfterCommit,
            async (sp, _, _, _) =>
            {
                dispatched.Add("handler-start");
                await FactoryEntryCall.RunAsync(sp, async () =>
                {
                    await sp.GetRequiredService<IFactoryEvents>().Raise(new HandlerFactoryFollowUpEvent("chained"));
                    return 0;
                });
                dispatched.Add("handler-after-factory");
            });
        FactoryEventHandlerRegistry.RegisterHandler<HandlerFactoryFollowUpEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit,
            (_, _, _, _) => { dispatched.Add("follow-up"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();

            await FactoryEntryCall.RunAsync(sp, async () =>
            {
                await events.Raise(new HandlerFactoryCallEvent("x"));
                return 0;
            });

            // The nested factory call completed inside the handler without draining
            // (follow-up runs after the handler, from the drain in progress).
            Assert.Equal(["handler-start", "handler-after-factory", "follow-up"], dispatched);
        }
    }

    [Fact]
    public void Run_NoSchedulerInScope_JustRunsTheBody()
    {
        // The sync mirror of RunAsync_NoSchedulerInScope — B-C3's client-reachable
        // unguarded shape (value-object factories) reduced to the body.
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        var result = FactoryEntryCall.Run(provider, () => 7);

        Assert.Equal(7, result);
    }

    [Fact]
    public async Task ConcurrentFlowsInOneScope_ShareEntryState_FailedFlowsWorkRidesTheSurvivingDrain()
    {
        // DOCUMENTED LIMITATION (plan review B-C2 follow-through): entry tracking is
        // per-SCOPE, not per-flow. Two concurrent flows in one scope interleave on the
        // same depth counter, so a failed flow's exit is a nested exit (no clear) and
        // the surviving flow's outermost drain runs BOTH flows' queued work. Scopes are
        // the framework's isolation unit — concurrent flows sharing a scope already
        // share DbContexts and every other scoped service. This test pins the actual
        // semantics so a change to them is a conscious one.
        var dispatched = new List<string>();
        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var scheduler = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            scheduler.BeginEntryCall();                       // flow A
            scheduler.BeginEntryCall();                       // flow B (interleaved)
            scheduler.Enqueue(DispatchPhase.AfterCommit, new FlowAEvent("a"), RaiseOptions.None,
                (_, _, _, _) => { dispatched.Add("flow-a"); return Task.CompletedTask; });
            scheduler.Enqueue(DispatchPhase.AfterCommit, new FlowBEvent("b"), RaiseOptions.None,
                (_, _, _, _) => { dispatched.Add("flow-b"); return Task.CompletedTask; });

            await scheduler.EndEntryCallAsync(success: false); // flow A fails — nested exit, no clear
            Assert.True(scheduler.HasPending);

            await scheduler.EndEntryCallAsync(success: true);  // flow B succeeds — drains both

            Assert.Equal(["flow-a", "flow-b"], dispatched);
            Assert.False(scheduler.HasPending);
            Assert.False(scheduler.IsEntryCallActive);
        }
    }

    private sealed record MisuseRecoveryEvent(string Value) : FactoryEventBase;
    private sealed record OceThrowingEvent(string Value) : FactoryEventBase;
    private sealed record OceRecoveryEvent(string Value) : FactoryEventBase;
    private sealed record CaughtInnerEvent(string Value) : FactoryEventBase;
    private sealed record CaughtOuterEvent(string Value) : FactoryEventBase;
    private sealed record HandlerFactoryCallEvent(string Value) : FactoryEventBase;
    private sealed record HandlerFactoryFollowUpEvent(string Value) : FactoryEventBase;
    private sealed record FlowAEvent(string Value) : FactoryEventBase;
    private sealed record FlowBEvent(string Value) : FactoryEventBase;

    private sealed class PhaseOceThrower { }
    private sealed class PhaseFactoryCallingHandler { }
}
