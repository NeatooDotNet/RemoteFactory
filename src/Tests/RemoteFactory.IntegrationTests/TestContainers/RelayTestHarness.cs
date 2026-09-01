using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.TestContainers;

/// <summary>
/// The one seam for relay-observing integration tests: client/server scopes wired to a
/// recording relay, and the wait that relay delivery being fire-and-forget makes
/// necessary.
/// </summary>
/// <remarks>
/// Consolidated in PHASE-007. Both <c>FactoryEventRelayTests</c> and
/// <c>FactoryEventCoalescingTests</c> had grown byte-identical copies of the wait plus
/// near-identical scope helpers with different arities, and each carried its own comment
/// apologising for <see cref="ClientServerContainers.Scopes(Action{IServiceCollection},
/// Action{IServiceCollection}, Action{IServiceCollection})"/>'s destructuring order.
/// Wrapping it once means that order is stated in exactly one place.
/// </remarks>
internal static class RelayTestHarness
{
    /// <summary>
    /// How long <see cref="WaitForAsync"/> waits before declaring the relay never
    /// arrived.
    /// </summary>
    /// <remarks>
    /// Generous on purpose. This is not a latency budget — a passing test returns the
    /// moment its predicate holds, so the value only bounds FAILURE. It used to be two
    /// seconds, which under a full-parallel run (default <c>-m</c>, every test class
    /// competing for the pool) was long enough to flake: relay delivery is a
    /// <c>Task.Run</c> continuation, and a saturated pool can leave one unscheduled well
    /// past two seconds with nothing actually wrong. Tightening a timeout to catch
    /// "slow" here would be measuring the machine, not the framework.
    /// </remarks>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Client, server and a recording relay registered on the client — the shape every
    /// relay test needs, in <c>(server, client, relay)</c> order.
    /// </summary>
    public static (IServiceScope server, IServiceScope client, RecordingFactoryEventRelay relay) ScopesWithRelay()
    {
        var relay = new RecordingFactoryEventRelay();

        // NOTE the destructuring: this ClientServerContainers overload returns
        // (client, server, local), unlike Scopes() and ScopesWithLogging() which return
        // (server, client, local). ClientServerContainersOrderTests pins all three.
        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services => services.AddSingleton<IFactoryEventRelay>(relay));

        return (server, client, relay);
    }

    /// <summary>
    /// Waits for <paramref name="predicate"/> to hold, then returns; throws if it never
    /// does.
    /// </summary>
    /// <remarks>
    /// Throwing is the point. The previous copies returned quietly on timeout, which
    /// turned any test whose follow-up assertion was an ABSENCE into a tautology:
    /// <c>ServerOnlyEvent_ExcludedFromRelayBatch</c> waits for the relay to fire and then
    /// asserts the batch is empty, and a relay that never fired at all satisfies that
    /// just as well. A timeout now fails as a timeout, naming what it was waiting for,
    /// instead of surfacing as a confusing assertion failure two lines later — or as no
    /// failure at all.
    /// </remarks>
    /// <param name="predicate">Checked immediately, then on each poll.</param>
    /// <param name="description">What the caller is waiting for, used in the failure.</param>
    /// <param name="timeout">Overrides the default failure bound.</param>
    public static async Task WaitForAsync(Func<bool> predicate, string description, TimeSpan? timeout = null)
    {
        var limit = timeout ?? DefaultTimeout;
        var deadline = DateTime.UtcNow + limit;

        while (true)
        {
            if (predicate())
            {
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Timed out after {limit.TotalSeconds:0.#}s waiting for: {description}. " +
                    "Relay delivery is fire-and-forget, so this means it never arrived — " +
                    "not that it arrived late.");
            }

            await Task.Delay(5);
        }
    }
}
