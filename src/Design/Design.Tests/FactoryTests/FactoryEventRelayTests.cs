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
                #region events-relay-register
                // Consumer replaces the NoOp default by registering their own relay.
                services.AddSingleton<IFactoryEventRelay>(aggregator);
                #endregion
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
    /// The client creates a checkout locally and saves it; the server's [Remote, Insert]
    /// raises the event, which is captured in the response and passed to the consumer's
    /// IFactoryEventRelay.Relay.
    /// </summary>
    [Fact]
    public async Task Relay_EventRaisedInRemoteInsert_DeliveredToConsumer()
    {
        var (server, client, relay) = ScopesWithRelay();

        try
        {
            var factory = client.GetRequiredService<ICheckoutOrderFactory>();
            var checkout = factory.Create(42, 99.95m);
            await factory.Save(checkout);

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
    /// <remarks>
    /// The checkout's Update raises ServerOnly; its Insert raises the relayed event.
    /// The test waits for the Insert's event before saving again, so the one event
    /// already received is the Insert's, and no second one may arrive.
    /// </remarks>
    [Fact]
    public async Task Relay_ServerOnlyFlag_EventNotRelayedToConsumer()
    {
        var (server, client, relay) = ScopesWithRelay();

        try
        {
            var factory = client.GetRequiredService<ICheckoutOrderFactory>();
            var checkout = await factory.Save(factory.Create(7, 5.00m));
            await WaitForRelayAsync(() => relay.ReceivedOfType<OrderCheckoutCompleted>().Any());
            Assert.Single(relay.ReceivedOfType<OrderCheckoutCompleted>());
            Assert.False(checkout.IsNew);

            checkout.Total = 6.00m;
            await factory.Save(checkout);

            // Give the fire-and-forget relay a chance to run, then assert the Update's event did not arrive.
            await Task.Delay(50);
            var received = Assert.Single(relay.ReceivedOfType<OrderCheckoutCompleted>());
            Assert.Equal(5.00m, received.Total);
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
            #region events-client-raise
            var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();
            await events.Raise(new ClientRaisedNotice(88));
            #endregion

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
            // Must not throw — NoOp relay silently drops the batch. Save is the remote
            // call whose response carries the batch; Create is local and never reaches it.
            await factory.Save(factory.Create(1, 10m));
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }

    /// <summary>
    /// The checkout's writes pass the save's token to Raise, so the server handler gets
    /// the caller's token: for the Insert's raise, and for the Update's ServerOnly raise,
    /// which runs the handler though nothing is relayed.
    /// </summary>
    [Fact]
    public async Task Save_PassesTheCallersToken_ToTheServerHandler()
    {
        var ledger = new TokenRecordingLedger();
        var (server, client, _) = DesignClientServerContainers.Scopes(
            configureServer: services => services.AddSingleton<ICheckoutLedger>(ledger));

        try
        {
            var factory = client.GetRequiredService<ICheckoutOrderFactory>();
            using var cts = new CancellationTokenSource();

            var checkout = await factory.Save(factory.Create(11, 20.00m), cts.Token);   // Insert
            checkout.Total = 21.00m;
            await factory.Save(checkout, cts.Token);                                    // Update, ServerOnly

            Assert.Equal([(11, 20.00m), (11, 21.00m)], ledger.Entries.Select(e => (e.OrderId, e.Total)));
            Assert.All(ledger.Entries, e => Assert.False(e.Token.IsCancellationRequested));

            cts.Cancel();
            Assert.All(ledger.Entries, e => Assert.True(e.Token.IsCancellationRequested));
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }

    private sealed class TokenRecordingLedger : ICheckoutLedger
    {
        public List<(int OrderId, decimal Total, CancellationToken Token)> Entries { get; } = [];

        public Task RecordAsync(int orderId, decimal total, CancellationToken cancellationToken)
        {
            Entries.Add((orderId, total, cancellationToken));
            return Task.CompletedTask;
        }
    }
}
