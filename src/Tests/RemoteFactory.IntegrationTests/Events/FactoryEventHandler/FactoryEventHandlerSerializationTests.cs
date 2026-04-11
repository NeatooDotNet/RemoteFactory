using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests for complex serialization, multi-event sequences, and end-to-end await semantics.
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

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "HandlerA" && e.EntityId == orderId);
        Assert.Contains(recorded, e => e.EventName.StartsWith("InventoryHandler:100") && e.EntityId == productId);
    }

    // =========================================================================
    // End-to-end await — by the time Raise returns, every handler has run
    // =========================================================================

    [Fact]
    public async Task ServerRaise_SlowHandler_CompletedBeforeRaiseReturns()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();

        // TestSlowHandler delays 300ms. Sequential awaited dispatch means the slow
        // handler must have recorded by the time Raise returns — no timing assertion
        // padding required.
        await events.Raise(new TestOrderEvent(orderId, "await@test.com"));

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "SlowHandler" && e.EntityId == orderId);
    }

    [Fact]
    public async Task ClientRaise_SlowHandler_CompletedBeforeRaiseReturns()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var orderId = Guid.NewGuid();

        // Across the wire: the client awaits the HTTP response, and the server
        // awaits every handler before responding, so a slow server handler must
        // have recorded before the client's await returns.
        await events.Raise(new TestOrderEvent(orderId, "client-await@test.com"));

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "SlowHandler" && e.EntityId == orderId);
    }

    [Fact]
    public async Task ClientRaise_ServerHandlerThrows_ExceptionSurfacesToClient()
    {
        var (client, server, local) = CreateScopes();
        var events = client.GetRequiredService<IFactoryEvents>();

        var id = Guid.NewGuid();

        // The server dispatches handlers sequentially inside the request, so a
        // throwing handler aborts the chain and the HTTP response carries the
        // failure. The in-memory stand-in preserves the original exception type
        // and message; over real HTTP the client would see an HttpRequestException
        // wrapping the server's 500 response (not covered by this suite — see
        // WebApplicationExtensions for the endpoint plumbing).
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => events.Raise(new TestFailingEvent(id, ShouldThrow: true)));
        Assert.Contains(id.ToString(), ex.Message);
    }
}
