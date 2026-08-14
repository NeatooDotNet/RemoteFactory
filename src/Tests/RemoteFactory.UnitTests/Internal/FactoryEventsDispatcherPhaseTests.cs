using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Covers what <see cref="IFactoryEvents.Raise{T}"/> does with phase-registered handlers:
/// Immediate dispatches as it always has, other phases defer to the scope's queue.
/// </summary>
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

            await events.Raise(new DeferredOnlyEvent("x"));

            lock (Dispatched)
            {
                Assert.Empty(Dispatched);
            }
            Assert.True(queue.HasPending);

            await queue.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

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

            await events.Raise(new MixedPhaseEvent("x"));

            lock (Dispatched)
            {
                Assert.Equal(["immediate"], Dispatched);
            }

            await queue.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

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

            await events.RaiseUntyped(new UntypedRaiseEvent("x"));

            lock (Dispatched)
            {
                Assert.Empty(Dispatched);
            }
            Assert.True(queue.HasPending);

            await queue.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

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
            });

            await events.Raise(new ChainedSourceEvent("x"));
            await queue.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

            lock (Dispatched)
            {
                Assert.Equal(["source", "follow-up"], Dispatched);
            }
            Assert.False(queue.HasPending);
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

            await events.Raise(new RelayCollectionEvent("x"));

            var collected = Assert.Single(collector.GetCollectedEvents());
            Assert.IsType<RelayCollectionEvent>(collected);
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

            await events.Raise(new RelayCollectionEvent("x"), RaiseOptions.ServerOnly);

            Assert.Empty(collector.GetCollectedEvents());
        }
    }
}
