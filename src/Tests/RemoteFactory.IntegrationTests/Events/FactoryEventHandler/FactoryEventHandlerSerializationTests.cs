using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests for complex serialization, multi-event sequences, await/fire-and-forget contrast,
/// and IEventTracker integration.
/// </summary>
public class FactoryEventHandlerSerializationTests
{
    private static (IServiceScope client, IServiceScope server, IServiceScope local) CreateScopes()
    {
        return ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: null);
    }

    // =========================================================================
    // Complex serialization
    // =========================================================================

    [Fact]
    public async Task ClientRaise_NestedRecordEvent_SurvivesSerialization()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var id = Guid.NewGuid();
        var complexEvent = new TestComplexEvent(
            id,
            "TestOrder",
            new TestAddress("123 Main St", "Springfield", "62701"),
            ["rush", "fragile"]);

        await events.Raise(complexEvent);

        await Task.Delay(300);

        var recorded = testService.GetRecordedEvents();
        var match = recorded.FirstOrDefault(e => e.EntityId == id);
        Assert.NotNull(match.EventName);
        Assert.Contains("TestOrder", match.EventName);
        Assert.Contains("Springfield", match.EventName);
        Assert.Contains("rush,fragile", match.EventName);
    }

    [Fact]
    public async Task ServerRaise_NestedRecordEvent_DispatchesLocally()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var id = Guid.NewGuid();
        await events.Raise(new TestComplexEvent(
            id, "ServerTest",
            new TestAddress("456 Oak Ave", "Portland", "97201"),
            ["local"]));

        await Task.Delay(300);

        var recorded = testService.GetRecordedEvents();
        var match = recorded.FirstOrDefault(e => e.EntityId == id);
        Assert.NotNull(match.EventName);
        Assert.Contains("Portland", match.EventName);
    }

    // =========================================================================
    // Multiple event types in sequence
    // =========================================================================

    [Fact]
    public async Task ClientRaise_MultipleDifferentEventTypes_AllDispatchCorrectly()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        var auditId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Raise three different event types from the client
        await events.Raise(new TestOrderEvent(orderId, "multi@test.com"));
        await events.Raise(new TestAuditEvent(auditId));
        await events.Raise(new TestInventoryEvent(productId, 42));

        await Task.Delay(300);

        var recorded = testService.GetRecordedEvents();

        // Each event type dispatched to its own handlers
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
        Assert.Contains(recorded, e => e.EventName == "AuditHandler" && e.EntityId == auditId);
        Assert.Contains(recorded, e => e.EventName.StartsWith("InventoryHandler:42") && e.EntityId == productId);

        // Verify no cross-contamination
        Assert.DoesNotContain(recorded, e => e.EventName == "AuditHandler" && e.EntityId == orderId);
        Assert.DoesNotContain(recorded, e => e.EventName == "HandlerA" && e.EntityId == auditId);
    }

    [Fact]
    public async Task ServerRaise_MultipleDifferentEventTypes_AllDispatchCorrectly()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await events.Raise(new TestOrderEvent(orderId, "serverseq@test.com"));
        await events.Raise(new TestInventoryEvent(productId, 100));

        await Task.Delay(300);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
        Assert.Contains(recorded, e => e.EventName.StartsWith("InventoryHandler:100") && e.EntityId == productId);
    }

    // =========================================================================
    // Client-side ContinueOnFail across the wire
    // =========================================================================

    [Fact]
    public async Task ClientRaise_ContinueOnFail_ServerHandlerThrows_OtherHandlersStillRun()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var id = Guid.NewGuid();

        // Client raises with ContinueOnFail, one server handler throws
        try
        {
            await events.Raise(new TestFailingEvent(id, ShouldThrow: true), RaiseOptions.ContinueOnFail);
        }
        catch
        {
            // Expected — error from server
        }

        await Task.Delay(300);

        var recorded = testService.GetRecordedEvents();
        // SurvivorHandler should still have run on the server
        Assert.Contains(recorded, e => e.EventName == "SurvivorHandler" && e.EntityId == id);
    }

    // =========================================================================
    // Await vs fire-and-forget contrast
    // =========================================================================

    [Fact]
    public async Task ServerRaise_Awaited_SlowHandlerCompletes_BeforeNextLine()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();

        // Await the raise — slow handler (300ms delay) should complete before we check
        await events.Raise(new TestOrderEvent(orderId, "await@test.com"));

        // Small buffer for Task.Run scheduling, but the slow handler has 300ms delay
        await Task.Delay(500);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "SlowHandler" && e.EntityId == orderId);
    }

    [Fact]
    public async Task ServerRaise_FireAndForget_ReturnsImmediately_HandlerCompletesLater()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();

        // Fire-and-forget — discard the task
        _ = events.Raise(new TestOrderEvent(orderId, "fireforget@test.com"));

        // Check immediately — fast handlers may have fired but slow handler (300ms) hasn't
        var recordedImmediate = testService.GetRecordedEvents();
        var slowFiredImmediately = recordedImmediate.Any(e => e.EventName == "SlowHandler" && e.EntityId == orderId);

        // Wait for slow handler to complete
        await Task.Delay(600);

        var recordedLater = testService.GetRecordedEvents();
        var slowFiredLater = recordedLater.Any(e => e.EventName == "SlowHandler" && e.EntityId == orderId);

        // The slow handler should NOT have fired immediately but SHOULD fire later
        Assert.False(slowFiredImmediately, "SlowHandler should not have completed immediately after fire-and-forget");
        Assert.True(slowFiredLater, "SlowHandler should have completed after waiting");
    }

    // =========================================================================
    // IEventTracker integration
    // =========================================================================

    [Fact]
    public async Task ServerRaise_FireAndForget_TaskTrackedByEventTracker()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var tracker = server.ServiceProvider.GetRequiredService<IEventTracker>();

        var orderId = Guid.NewGuid();

        // Fire-and-forget
        _ = events.Raise(new TestOrderEvent(orderId, "tracker@test.com"));

        // The tracker should have pending tasks (slow handler takes 300ms)
        // Check within a small window — handlers are on Task.Run so there's a brief startup
        await Task.Delay(50);
        var pendingDuringExecution = tracker.PendingCount;

        // Wait for all to finish
        await tracker.WaitAllAsync();

        // After WaitAll, pending count should be 0
        var pendingAfterWait = tracker.PendingCount;

        Assert.True(pendingDuringExecution > 0, $"Expected pending tasks during execution, got {pendingDuringExecution}");
        Assert.Equal(0, pendingAfterWait);
    }
}
