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
        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var scheduler = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.EndEntryCallAsync(success: true));

            // The failure path runs inside catch blocks and must never throw.
            await scheduler.EndEntryCallAsync(success: false);
        }
    }
}
