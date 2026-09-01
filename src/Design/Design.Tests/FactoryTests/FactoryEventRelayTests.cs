using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for the factory event relay pattern.
/// Verifies that events raised on the server during factory operations are
/// captured in RemoteResponseDto and delivered to the consumer's
/// IFactoryEventRelay implementation fire-and-forget after the factory method
/// returns.
/// </summary>
public class FactoryEventRelayTests
{
    private static (IServiceScope server, IServiceScope client, InMemoryAggregatorRelay relay) ScopesWithRelay()
    {
        var aggregator = new InMemoryAggregatorRelay();
        var (server, client, _) = DesignClientServerContainers.Scopes(
            configureClient: services =>
            {
                // Consumer replaces the NoOp default by registering their own relay.
                services.AddSingleton<IFactoryEventRelay>(aggregator);
            });
        return (server, client, aggregator);
    }

    private static async Task WaitForRelayAsync(Func<bool> predicate, TimeSpan? timeout = null)
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
    /// Server raises an event during a [Remote, Create] method. The event is captured
    /// in the response and passed to the consumer's IFactoryEventRelay.Relay.
    /// </summary>
    [Fact]
    public async Task Relay_EventRaisedInRemoteCreate_DeliveredToConsumer()
    {
        var (server, client, relay) = ScopesWithRelay();

        try
        {
            var factory = client.GetRequiredService<ICheckoutOrderFactory>();
            await factory.Create(42, 99.95m);

            await WaitForRelayAsync(() => relay.ReceivedOfType<OrderCheckoutCompleted>().Any());

            var received = relay.ReceivedOfType<OrderCheckoutCompleted>().Single();
            Assert.Equal(42, received.OrderId);
            Assert.Equal(99.95m, received.Total);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }

    /// <summary>
    /// RaiseOptions.ServerOnly dispatches to server handlers but excludes the event
    /// from the relay. The consumer's Relay is still invoked once per factory call,
    /// but the batch is empty.
    /// </summary>
    [Fact]
    public async Task Relay_ServerOnlyFlag_EventNotRelayedToConsumer()
    {
        var (server, client, relay) = ScopesWithRelay();

        try
        {
            var factory = client.GetRequiredService<ICheckoutOrderFactory>();
            await factory.CreateWithServerOnlyEvent(7, 5.00m);

            // Give the fire-and-forget relay a chance to run, then assert nothing arrived.
            await Task.Delay(50);
            Assert.Empty(relay.ReceivedOfType<OrderCheckoutCompleted>());
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }

    /// <summary>
    /// A client-initiated Raise relays like a factory call: the caller's own event and
    /// everything its server handlers raise both come back in the response's batch.
    /// </summary>
    /// <remarks>
    /// The client's own event is in the batch deliberately — raising from the client
    /// dispatches nothing locally, so relaying it back is the only way client-side
    /// handlers see it. See FactoryEventRelayPattern.ClientRaisedNoticeHandler.
    /// <para>
    /// Both assertions are positive, so a quiet <see cref="WaitForRelayAsync"/> timeout
    /// still fails the test rather than passing vacuously.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Relay_ClientRaisedEvent_DeliversOwnEventAndHandlerRaisedEvent()
    {
        var (server, client, relay) = ScopesWithRelay();

        try
        {
            var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();
            await events.Raise(new ClientRaisedNotice(88));

            await WaitForRelayAsync(() => relay.ReceivedOfType<NoticeAcknowledged>().Any());

            Assert.Equal(88, relay.ReceivedOfType<ClientRaisedNotice>().Single().OrderId);
            Assert.Equal(88, relay.ReceivedOfType<NoticeAcknowledged>().Single().OrderId);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }

    /// <summary>
    /// NoOp default: when the consumer registers no IFactoryEventRelay, RemoteFactory
    /// registers NoOpFactoryEventRelay (via TryAdd). Factory calls still succeed; no
    /// events are delivered.
    /// </summary>
    [Fact]
    public async Task Relay_NoConsumerRegistration_NoOpDefaultUsed()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();

        try
        {
            var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
            Assert.Equal("NoOpFactoryEventRelay", relay.GetType().Name);

            var factory = client.GetRequiredService<ICheckoutOrderFactory>();
            // Must not throw — NoOp relay silently drops the batch.
            await factory.Create(1, 10m);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }
}
