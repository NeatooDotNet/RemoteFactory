using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests that [FactoryEventHandler] mediator pattern coexists with [Event] delegate pattern.
/// </summary>
public class FactoryEventHandlerCoexistenceTests
{
    private static (IServiceScope client, IServiceScope server, IServiceScope local) CreateScopes()
    {
        return ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: null);
    }

    [Fact]
    public async Task BothPatterns_WorkIndependently_InSameCompilation()
    {
        var (client, server, local) = CreateScopes();
        var testService = server.GetRequiredService<IEventTestService>();

        // 1. Use existing [Event] delegate pattern
        var onWarehouse = server.GetRequiredService<OrderEventHandler.NotifyWarehouseEvent>();
        var eventOrderId = Guid.NewGuid();
        await onWarehouse(eventOrderId);

        // 2. Use new [FactoryEventHandler] mediator pattern
        var factoryEvents = server.GetRequiredService<IFactoryEvents>();
        var mediatorOrderId = Guid.NewGuid();
        await factoryEvents.Raise(new TestOrderEvent(mediatorOrderId, "coexist@test.com"));

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();

        // [Event] delegate fired
        Assert.Contains(recorded, e => e.EventName == "NotifyWarehouse" && e.EntityId == eventOrderId);

        // [FactoryEventHandler] mediator fired
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == mediatorOrderId);
        Assert.Contains(recorded, e => e.EventName == "HandlerB" && e.EntityId == mediatorOrderId);
    }
}
