using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests for [FactoryEventHandler] local dispatch (Server/Logical mode).
/// </summary>
public class FactoryEventHandlerLocalTests
{
    private static (IServiceScope server, IServiceScope client, IServiceScope local) CreateScopes()
    {
        return ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: null);
    }

    [Fact]
    public async Task Raise_SingleHandlerType_HandlerFires()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var entityId = Guid.NewGuid();
        await events.Raise(new TestAuditEvent(entityId));

        // Allow background tasks to complete
        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "AuditHandler" && e.EntityId == entityId);
    }

    [Fact]
    public async Task Raise_MultipleHandlers_AllFire()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        await events.Raise(new TestOrderEvent(orderId, "test@example.com"));

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
        Assert.Contains(recorded, e => e.EventName == "HandlerB" && e.EntityId == orderId);
    }

    [Fact]
    public async Task Raise_NoHandlers_CompletesWithoutError()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();

        // Should complete without throwing
        await events.Raise(new TestUnhandledEvent());
    }

    [Fact]
    public async Task Raise_DifferentEventTypes_StrictRouting()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var auditId = Guid.NewGuid();
        await events.Raise(new TestAuditEvent(auditId));

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        // AuditHandler should fire
        Assert.Contains(recorded, e => e.EventName == "AuditHandler" && e.EntityId == auditId);
        // OrderHandlers should NOT fire for TestAuditEvent
        Assert.DoesNotContain(recorded, e => e.EventName == "HandlerA");
        Assert.DoesNotContain(recorded, e => e.EventName == "HandlerB");
    }

    [Fact]
    public async Task Raise_EventProperties_ReceiveCorrectValues()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        var email = "verify@example.com";
        await events.Raise(new TestOrderEvent(orderId, email));

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        // HandlerA records the OrderId — verifies the event object arrived with correct data
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
    }

    [Fact]
    public async Task Raise_ServiceInjection_ServicesResolvedCorrectly()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        await events.Raise(new TestOrderEvent(orderId, "svc@test.com"));

        await Task.Delay(200);

        // If service injection failed, handlers would throw and no events would be recorded
        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
    }
}
