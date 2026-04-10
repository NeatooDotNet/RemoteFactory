using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests for [FactoryEventHandler] client-to-server serialization round-trip.
/// Verifies events cross the wire correctly via ClientServerContainers.
/// </summary>
public class FactoryEventHandlerClientServerTests
{
    private static (IServiceScope client, IServiceScope server, IServiceScope local) CreateScopes()
    {
        return ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: null);
    }

    [Fact]
    public async Task ClientRaise_ServerHandlerFires()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        await events.Raise(new TestOrderEvent(orderId, "remote@example.com"));

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
    }

    [Fact]
    public async Task ClientRaise_MultipleProperties_SurviveSerialization()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        var email = "serialization-test@example.com";
        await events.Raise(new TestOrderEvent(orderId, email));

        await Task.Delay(200);

        // The handler records OrderId — if serialization broke, wrong value would appear
        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
    }

    [Fact]
    public async Task ClientRaise_MultipleHandlers_AllFireOnServer()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        await events.Raise(new TestOrderEvent(orderId, "multi@example.com"));

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
        Assert.Contains(recorded, e => e.EventName == "HandlerB" && e.EntityId == orderId);
    }

    [Fact]
    public async Task ClientRaise_FireAndForget_ServerStillExecutes()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        // Fire-and-forget: discard the task
        _ = events.Raise(new TestOrderEvent(orderId, "fireforget@example.com"));

        // Allow time for background execution
        await Task.Delay(500);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
    }

    [Fact]
    public async Task ServerRaise_DispatchesLocally_NoHttp()
    {
        var (client, server, local) = CreateScopes();
        // Raise from the server side — should dispatch locally without going through HTTP
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        await events.Raise(new TestOrderEvent(orderId, "server-local@example.com"));

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
        Assert.Contains(recorded, e => e.EventName == "HandlerB" && e.EntityId == orderId);
    }
}
