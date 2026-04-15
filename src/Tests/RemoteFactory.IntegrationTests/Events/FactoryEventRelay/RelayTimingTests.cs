using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventRelay;

/// <summary>
/// Verifies the post-return ordering contract (plan rules 6, 7):
///
///     _assignTarget = await factory.Create(...);   // runs FIRST
///     // ... then ...
///     relay.Relay(events);                         // fires AFTER
///
/// A Relay implementation that reads the caller's <c>_assignTarget</c> must observe
/// the freshly-assigned value, not the pre-call value. This test would FAIL against
/// the old inline-dispatch behavior where the relay's synchronous prologue executed
/// before `return deserialized;` (and therefore before the caller's continuation
/// resumed).
///
/// Also verifies the guarantee holds in a NO-SyncContext host (plain ThreadPool).
/// </summary>
public class RelayTimingTests
{
    /// <summary>
    /// Relay that invokes a snapshot callback when it receives events. The callback
    /// reads whatever state the test cares about — typically the value of a local
    /// field assigned from the factory call's result.
    /// </summary>
    private sealed class SnapshotRelay : IFactoryEventRelay
    {
        private readonly Func<IReadOnlyList<FactoryEventBase>, object?> _snapshot;
        private readonly TaskCompletionSource<object?> _tcs = new();

        public SnapshotRelay(Func<IReadOnlyList<FactoryEventBase>, object?> snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<object?> Captured => _tcs.Task;

        public Task Relay(IReadOnlyList<FactoryEventBase> events)
        {
            try
            {
                _tcs.TrySetResult(_snapshot(events));
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Relay_FiresAfterCallerContinuation_InNoSyncContextHost()
    {
        // Force ThreadPool context — production Blazor host has a SyncContext; this test
        // covers the plan Risk #3 gap (non-Blazor hosts) that the requirements review flagged.
        await Task.Run(async () =>
        {
            Assert.Null(SynchronizationContext.Current);

            RelayTestResult? assignedAfterAwait = null;

            var snapshotRelay = new SnapshotRelay(_ => assignedAfterAwait);

            var (client, server, local) = ClientServerContainers.Scopes(
                configureClient: services => services.AddSingleton<IFactoryEventRelay>(snapshotRelay));

            try
            {
                var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();

                // THIS is the critical pattern. If Relay fires before the assignment below
                // executes, the snapshot will be null; the assertion catches it.
                assignedAfterAwait = await createDel("timing-test");

                var captured = await snapshotRelay.Captured.WaitAsync(TimeSpan.FromSeconds(2));

                Assert.NotNull(captured);
                Assert.Equal(assignedAfterAwait, captured);
            }
            finally
            {
                server.Dispose();
                client.Dispose();
                local.Dispose();
            }
        });
    }

    /// <summary>
    /// The old inline dispatch bug:
    ///     _ = _relay.DispatchRelayedEvents(...);
    ///     return deserialized;
    /// executed the relay's synchronous prologue (and any handler work that completed
    /// synchronously) BEFORE the factory's caller received the deserialized result.
    /// This test writes caller state immediately on continuation resume and asserts
    /// the relay observes it — something the old code path would violate because the
    /// relay's read would execute before the caller's write.
    /// </summary>
    [Fact]
    public async Task Relay_FiresAfterCallerSynchronousWriteOnContinuation()
    {
        string? callerState = null;
        var snapshotRelay = new SnapshotRelay(_ => callerState);

        var (client, server, local) = ClientServerContainers.Scopes(
            configureClient: services => services.AddSingleton<IFactoryEventRelay>(snapshotRelay));

        try
        {
            var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();

            var result = await createDel("synchronous-write");
            // Synchronous assignment immediately on continuation resume — this MUST
            // complete before Relay fires.
            callerState = $"assigned-{result.Id}";

            var captured = (string?)await snapshotRelay.Captured.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal($"assigned-{result.Id}", captured);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
            local.Dispose();
        }
    }
}
