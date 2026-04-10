using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for the factory event relay pattern.
/// Verifies that events raised on the server during factory operations are
/// captured in RemoteResponseDto and dispatched to client-side [FactoryEventHandler&lt;T&gt;]
/// instance handlers after the operation completes.
/// </summary>
public class FactoryEventRelayTests
{
    /// <summary>
    /// Server raises an event during a [Remote, Create] method. The event is captured
    /// in the response, relayed to the client, and dispatched to the registered handler.
    /// </summary>
    [Fact]
    public async Task Relay_EventRaisedInRemoteCreate_DispatchedToClientHandler()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();

        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        using var viewModel = new OrderCheckoutViewModel(relay);

        try
        {
            var factory = client.GetRequiredService<ICheckoutOrderFactory>();
            await factory.Create(42, 99.95m);

            Assert.Single(viewModel.ReceivedEvents);
            Assert.Equal(42, viewModel.ReceivedEvents[0].OrderId);
            Assert.Equal(99.95m, viewModel.ReceivedEvents[0].Total);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }

    /// <summary>
    /// RaiseOptions.ServerOnly dispatches to server handlers but excludes the event
    /// from the relay. The client handler must NOT receive the event.
    /// </summary>
    [Fact]
    public async Task Relay_ServerOnlyFlag_EventNotRelayedToClient()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();

        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        using var viewModel = new OrderCheckoutViewModel(relay);

        try
        {
            var factory = client.GetRequiredService<ICheckoutOrderFactory>();
            await factory.CreateWithServerOnlyEvent(7, 5.00m);

            // ServerOnly: client must not receive the event
            Assert.Empty(viewModel.ReceivedEvents);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }

    /// <summary>
    /// Unregister stops delivery. After Unregister, subsequent factory operations
    /// do not reach the handler.
    /// </summary>
    [Fact]
    public async Task Relay_Unregister_StopsDelivery()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();

        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        using var viewModel = new OrderCheckoutViewModel(relay);

        try
        {
            var factory = client.GetRequiredService<ICheckoutOrderFactory>();

            await factory.Create(1, 10m);
            Assert.Single(viewModel.ReceivedEvents);

            relay.Unregister(viewModel);

            await factory.Create(2, 20m);
            Assert.Single(viewModel.ReceivedEvents); // Still just the first
        }
        finally
        {
            server.Dispose();
            client.Dispose();
        }
    }
}
