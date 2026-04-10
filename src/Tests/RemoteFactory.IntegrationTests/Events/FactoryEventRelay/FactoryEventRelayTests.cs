using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventRelay;

/// <summary>
/// Integration tests for the factory event relay pattern:
/// Server raises events -> captured in response -> relayed to client handlers.
/// </summary>
public class FactoryEventRelayTests
{
    [Fact]
    public async Task SingleEventRelay_ClientHandlerReceivesEvent()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        var handler = new TestRelayHandler();
        relay.Register(handler);

        try
        {
            var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();
            var result = await createDel("test");

            Assert.Single(handler.ReceivedEvents);
            Assert.Equal(result.Id, handler.ReceivedEvents[0].Id);
            Assert.Equal("Created: test", handler.ReceivedEvents[0].Message);
        }
        finally
        {
            relay.Unregister(handler);
        }
    }

    [Fact]
    public async Task MultipleEventsRelay_AllEventsReceivedInOrder()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        var handlerA = new TestRelayHandler();
        var handlerB = new TestRelayEventBHandler();
        relay.Register(handlerA);
        relay.Register(handlerB);

        try
        {
            var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.CreateWithMultipleEvents>();
            await createDel("test");

            Assert.Single(handlerA.ReceivedEvents);
            Assert.Equal("First", handlerA.ReceivedEvents[0].Message);

            Assert.Single(handlerB.ReceivedEvents);
            Assert.Equal(2, handlerB.ReceivedEvents[0].Sequence);
        }
        finally
        {
            relay.Unregister(handlerA);
            relay.Unregister(handlerB);
        }
    }

    [Fact]
    public async Task ServerOnlyEvent_NotRelayedToClient()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        var handler = new TestRelayHandler();
        relay.Register(handler);

        try
        {
            var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.CreateWithServerOnlyEvent>();
            await createDel("test");

            Assert.Empty(handler.ReceivedEvents);
        }
        finally
        {
            relay.Unregister(handler);
        }
    }

    [Fact]
    public async Task NoEvents_NoRelayedEvents()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        var handler = new TestRelayHandler();
        relay.Register(handler);

        try
        {
            var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.CreateNoEvents>();
            await createDel("test");
            Assert.Empty(handler.ReceivedEvents);
        }
        finally
        {
            relay.Unregister(handler);
        }
    }

    [Fact]
    public async Task MultipleHandlersSameEvent_AllHandlersInvoked()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        var handlerA = new TestRelayHandler();
        var handlerB = new TestRelayHandlerB();
        relay.Register(handlerA);
        relay.Register(handlerB);

        try
        {
            var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();
            var result = await createDel("test");

            Assert.Single(handlerA.ReceivedEvents);
            Assert.Single(handlerB.ReceivedEvents);
            Assert.Equal(result.Id, handlerA.ReceivedEvents[0].Id);
            Assert.Equal(result.Id, handlerB.ReceivedEvents[0].Id);
        }
        finally
        {
            relay.Unregister(handlerA);
            relay.Unregister(handlerB);
        }
    }

    [Fact]
    public async Task UnregisterStopsDelivery()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        var handler = new TestRelayHandler();
        relay.Register(handler);

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();

        await createDel("first");
        Assert.Single(handler.ReceivedEvents);

        relay.Unregister(handler);

        await createDel("second");
        Assert.Single(handler.ReceivedEvents); // Still just one
    }

    [Fact]
    public async Task HandlerException_DoesNotPropagateToFactoryCaller()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        var throwingHandler = new TestRelayThrowingHandler();
        var goodHandler = new TestRelayHandler();
        relay.Register(throwingHandler);
        relay.Register(goodHandler);

        try
        {
            var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();
            var result = await createDel("test");

            Assert.NotNull(result);
            Assert.Equal("test", result.Name);
            Assert.Single(goodHandler.ReceivedEvents);
        }
        finally
        {
            relay.Unregister(throwingHandler);
            relay.Unregister(goodHandler);
        }
    }

    [Fact]
    public async Task NoRegisteredHandlers_EventSilentlyDropped()
    {
        var (server, client, local) = ClientServerContainers.Scopes();

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();
        var result = await createDel("test");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ServerOnlyCombinedWithContinueOnFail_NotRelayed()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();
        var handler = new TestRelayHandler();
        relay.Register(handler);

        try
        {
            var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.CreateWithServerOnlyCombinedFlags>();
            await createDel("test");
            Assert.Empty(handler.ReceivedEvents);
        }
        finally
        {
            relay.Unregister(handler);
        }
    }

    [Fact]
    public async Task WeakReferenceCleanup_GarbageCollectedHandlerRemoved()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var relay = client.ServiceProvider.GetRequiredService<IFactoryEventRelay>();

        RegisterWeakHandler(relay);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var createDel = client.ServiceProvider.GetRequiredService<RelayTestCommands.Create>();
        var result = await createDel("test");
        Assert.NotNull(result);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void RegisterWeakHandler(IFactoryEventRelay relay)
    {
        var handler = new TestRelayHandler();
        relay.Register(handler);
    }
}
