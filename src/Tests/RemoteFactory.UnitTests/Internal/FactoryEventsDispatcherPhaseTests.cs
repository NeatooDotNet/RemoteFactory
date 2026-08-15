using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Covers what <see cref="IFactoryEvents.Raise{T}"/> does with phase-registered handlers:
/// Immediate dispatches as it always has; other phases defer to the scope's scheduler
/// while an entry factory call is active (PHASE-003) and dispatch immediately otherwise.
/// </summary>
/// <remarks>
/// PHASE-003 amended the deferral tests here to raise inside an active entry call
/// (<see cref="IFactoryEventPhaseScheduler.BeginEntryCall"/>): PHASE-001's interim
/// behavior — queue whenever a scheduler exists — was chartered to be inverted by the
/// entry-call work, and each test's original intent (defer at raise time, dispatch at
/// the drain, cross-phase ordering, RaiseUntyped parity) is restated under entry
/// semantics rather than removed.
/// </remarks>
public class FactoryEventsDispatcherPhaseTests
{
    private sealed record ImmediateOnlyEvent(string Value) : FactoryEventBase;
    private sealed record DeferredOnlyEvent(string Value) : FactoryEventBase;
    private sealed record MixedPhaseEvent(string Value) : FactoryEventBase;
    private sealed record RelayCollectionEvent(string Value) : FactoryEventBase;
    private sealed record UntypedRaiseEvent(string Value) : FactoryEventBase;
    private sealed record ChainedSourceEvent(string Value) : FactoryEventBase;
    private sealed record ChainedFollowUpEvent(string Value) : FactoryEventBase;
    private sealed record NoQueueEvent(string Value) : FactoryEventBase;
    private sealed record OutsideEntryEvent(string Value) : FactoryEventBase;
    private sealed record FailedEntryEvent(string Value) : FactoryEventBase;
    private sealed record SecondEntryEvent(string Value) : FactoryEventBase;

    private sealed class ImmediateHandler { }
    private sealed class DeferredHandler { }

    private static readonly List<string> Dispatched = [];

    private static Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Recording(string name)
        => (_, _, _, _) =>
        {
            lock (Dispatched)
            {
                Dispatched.Add(name);
            }
            return Task.CompletedTask;
        };

    private static (ServiceProvider Provider, IServiceScope Scope) ServerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventsDispatcherPhaseTests).Assembly);
        var provider = services.BuildServiceProvider();
        return (provider, provider.CreateScope());
    }

    [Fact]
    public async Task Raise_ImmediateHandler_DispatchesAtRaiseTime()
    {
        lock (Dispatched) { Dispatched.Clear(); }
        FactoryEventHandlerRegistry.RegisterHandler<ImmediateOnlyEvent>(typeof(ImmediateHandler), DispatchPhase.Immediate, Recording("immediate"));

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();

            await events.Raise(new ImmediateOnlyEvent("x"));

            lock (Dispatched)
            {
                Assert.Equal(["immediate"], Dispatched);
            }
        }
    }

    [Fact]
    public async Task Raise_DeferredHandler_DoesNotDispatchAtRaiseTime()
    {
        lock (Dispatched) { Dispatched.Clear(); }
        FactoryEventHandlerRegistry.RegisterHandler<DeferredOnlyEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("deferred"));

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
            var queue = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            queue.BeginEntryCall();
            await events.Raise(new DeferredOnlyEvent("x"));

            lock (Dispatched)
            {
                Assert.Empty(Dispatched);
            }
            Assert.True(queue.HasPending);

            await queue.EndEntryCallAsync(success: true);

            lock (Dispatched)
            {
                Assert.Equal(["deferred"], Dispatched);
            }
        }
    }

    [Fact]
    public async Task Raise_MixedPhases_ImmediateRunsAndDeferredWaits()
    {
        lock (Dispatched) { Dispatched.Clear(); }
        FactoryEventHandlerRegistry.RegisterHandler<MixedPhaseEvent>(typeof(ImmediateHandler), DispatchPhase.Immediate, Recording("immediate"));
        FactoryEventHandlerRegistry.RegisterHandler<MixedPhaseEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("deferred"));

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
            var queue = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            queue.BeginEntryCall();
            await events.Raise(new MixedPhaseEvent("x"));

            lock (Dispatched)
            {
                Assert.Equal(["immediate"], Dispatched);
            }

            await queue.EndEntryCallAsync(success: true);

            // Cross-phase ordering: the Immediate handler completed before the deferred one ran.
            lock (Dispatched)
            {
                Assert.Equal(["immediate", "deferred"], Dispatched);
            }
        }
    }

    [Fact]
    public async Task RaiseUntyped_DeferredHandler_DefersJustLikeRaise()
    {
        // RaiseUntyped is the path client-raised events take server-side, so it is the
        // entry point for the most interesting phase case.
        lock (Dispatched) { Dispatched.Clear(); }
        FactoryEventHandlerRegistry.RegisterHandler<UntypedRaiseEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("deferred"));

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
            var queue = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            queue.BeginEntryCall();
            await events.RaiseUntyped(new UntypedRaiseEvent("x"));

            lock (Dispatched)
            {
                Assert.Empty(Dispatched);
            }
            Assert.True(queue.HasPending);

            await queue.EndEntryCallAsync(success: true);

            lock (Dispatched)
            {
                Assert.Equal(["deferred"], Dispatched);
            }
        }
    }

    [Fact]
    public async Task DrainedHandlerRaisingAnEvent_GoesThroughTheRealRaisePath()
    {
        // Re-entrancy as production hits it: handler -> IFactoryEvents.Raise -> registry
        // lookup -> defer, rather than calling Enqueue directly.
        //
        // PHASE-003 re-pointed this test to discriminate on WHERE the follow-up runs.
        // The entry stays active for the duration of the entry drain, so the follow-up
        // raised by the draining source handler must QUEUE and run after the source
        // handler completes ("source-after-raise" before "follow-up"). If entry depth
        // popped before the drain, the follow-up would dispatch inline inside the
        // source handler's Raise call and the order would invert.
        lock (Dispatched) { Dispatched.Clear(); }

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
            var queue = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            FactoryEventHandlerRegistry.RegisterHandler<ChainedFollowUpEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("follow-up"));
            FactoryEventHandlerRegistry.RegisterHandler<ChainedSourceEvent>(typeof(ImmediateHandler), DispatchPhase.AfterCommit, async (sp, _, _, ct) =>
            {
                lock (Dispatched)
                {
                    Dispatched.Add("source");
                }
                await sp.GetRequiredService<IFactoryEvents>().Raise(new ChainedFollowUpEvent("chained"), RaiseOptions.None, ct);
                lock (Dispatched)
                {
                    Dispatched.Add("source-after-raise");
                }
            });

            queue.BeginEntryCall();
            await events.Raise(new ChainedSourceEvent("x"));
            await queue.EndEntryCallAsync(success: true);

            lock (Dispatched)
            {
                Assert.Equal(["source", "source-after-raise", "follow-up"], Dispatched);
            }
            Assert.False(queue.HasPending);
        }
    }

    [Fact]
    public async Task Raise_PhasedHandlerOutsideAnyFactoryCall_DispatchesImmediately()
    {
        // A scheduler exists in the scope, but no entry factory call is active — the
        // "Raise outside any factory call" case. The phased handler dispatches
        // immediately, with the 9005 debug log positively pinned here (an absence
        // assertion elsewhere cannot pin an emission).
        lock (Dispatched) { Dispatched.Clear(); }
        FactoryEventHandlerRegistry.RegisterHandler<OutsideEntryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("deferred"));

        var capture = new CapturingProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(capture).SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventsDispatcherPhaseTests).Assembly);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
        var queue = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

        await events.Raise(new OutsideEntryEvent("x"));

        lock (Dispatched)
        {
            Assert.Equal(["deferred"], Dispatched);
        }
        Assert.False(queue.HasPending);
        lock (capture.Entries)
        {
            Assert.Contains(capture.Entries, e =>
                e.EventId == 9005 && e.Level == Microsoft.Extensions.Logging.LogLevel.Debug);
        }
    }

    private sealed class CapturingProvider : Microsoft.Extensions.Logging.ILoggerProvider
    {
        public List<(int EventId, Microsoft.Extensions.Logging.LogLevel Level)> Entries { get; } = [];

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose() { }

        private sealed class CapturingLogger(CapturingProvider owner) : Microsoft.Extensions.Logging.ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (owner.Entries)
                {
                    owner.Entries.Add((eventId.Id, logLevel));
                }
            }
        }
    }

    [Fact]
    public async Task FailedEntryCall_ClearsDeferredWork_AndTheNextSuccessRunsOnlyItsOwn()
    {
        // The long-lived-scope case (plan review A-V2): a failed entry call's deferred
        // work must not ride into the next successful call's drain in the same scope.
        lock (Dispatched) { Dispatched.Clear(); }
        FactoryEventHandlerRegistry.RegisterHandler<FailedEntryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("from-failed-call"));
        FactoryEventHandlerRegistry.RegisterHandler<SecondEntryEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("from-second-call"));

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
            var queue = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            // First entry call defers work, then fails.
            queue.BeginEntryCall();
            await events.Raise(new FailedEntryEvent("x"));
            Assert.True(queue.HasPending);
            await queue.EndEntryCallAsync(success: false);

            Assert.False(queue.HasPending);
            lock (Dispatched)
            {
                Assert.Empty(Dispatched);
            }

            // Second entry call in the SAME scope succeeds and drains only its own work.
            queue.BeginEntryCall();
            await events.Raise(new SecondEntryEvent("y"));
            await queue.EndEntryCallAsync(success: true);

            lock (Dispatched)
            {
                Assert.Equal(["from-second-call"], Dispatched);
            }
        }
    }

    [Fact]
    public async Task Raise_PhasedHandlerWithNoQueueInScope_DispatchesImmediatelyRatherThanVanishing()
    {
        lock (Dispatched) { Dispatched.Clear(); }
        FactoryEventHandlerRegistry.RegisterHandler<NoQueueEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("deferred"));

        // A container without the phase queue registered — the fallback path in
        // FactoryEventsDispatcher.
        var services = new ServiceCollection();
        services.AddLogging();
        using var provider = services.BuildServiceProvider();
        var dispatcher = new FactoryEventsDispatcher(provider);

        await dispatcher.Raise(new NoQueueEvent("x"));

        lock (Dispatched)
        {
            Assert.Equal(["deferred"], Dispatched);
        }
    }

    [Fact]
    public async Task Raise_DeferredHandler_StillCollectsForRelayAtRaiseTime()
    {
        FactoryEventHandlerRegistry.RegisterHandler<RelayCollectionEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("deferred"));

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
            var collector = scope.ServiceProvider.GetRequiredService<IFactoryEventCollector>();
            var queue = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            // Entry active so the handler genuinely DEFERS — collection at raise time
            // is only meaningful while the dispatch hasn't happened yet.
            queue.BeginEntryCall();
            await events.Raise(new RelayCollectionEvent("x"));

            var collected = Assert.Single(collector.GetCollectedEvents());
            Assert.IsType<RelayCollectionEvent>(collected);
            Assert.True(queue.HasPending);
        }
    }

    [Fact]
    public async Task Raise_DeferredHandlerWithServerOnly_IsNotCollectedForRelay()
    {
        FactoryEventHandlerRegistry.RegisterHandler<RelayCollectionEvent>(typeof(DeferredHandler), DispatchPhase.AfterCommit, Recording("deferred"));

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
            var collector = scope.ServiceProvider.GetRequiredService<IFactoryEventCollector>();
            var queue = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

            // Entry active — see Raise_DeferredHandler_StillCollectsForRelayAtRaiseTime.
            queue.BeginEntryCall();
            await events.Raise(new RelayCollectionEvent("x"), RaiseOptions.ServerOnly);

            Assert.Empty(collector.GetCollectedEvents());
            // The deferral premise, asserted so it cannot vacate silently again.
            Assert.True(queue.HasPending);
        }
    }
}
