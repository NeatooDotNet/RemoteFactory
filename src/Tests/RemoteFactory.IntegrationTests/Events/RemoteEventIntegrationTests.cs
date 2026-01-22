#pragma warning disable xUnit1051 // Test timing delays don't need test cancellation token

using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events;

/// <summary>
/// Integration tests for remote event invocation using the two DI container pattern.
/// These tests verify the full client to server round-trip for event delegates.
/// </summary>
public class RemoteEventIntegrationTests
{
    /// <summary>
    /// Verifies that an event invoked from the client container is executed on the server.
    /// Uses the serialization round-trip to simulate HTTP transport.
    /// </summary>
    [Fact]
    public async Task RemoteEvent_ClientToServer_ExecutesOnServer()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();

        // Get the event delegate from the client scope
        var sendConfirmation = scopes.client.ServiceProvider
            .GetRequiredService<OrderEventTarget.SendOrderConfirmationEvent>();

        // Access the server's container through the ServerServiceProvider
        var serverProvider = scopes.client.ServiceProvider
            .GetRequiredService<ServerServiceProvider>()
            .serverProvider;

        // The server has IEventTestService registered via RegisterMatchingName
        var testService = serverProvider.GetRequiredService<IEventTestService>();
        testService.Clear();

        // Act
        var orderId = Guid.NewGuid();
        await sendConfirmation(orderId);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(100);

        // Assert
        var recordedEvents = testService.GetRecordedEvents();
        Assert.Contains(recordedEvents, e => e.EventName == "SendConfirmation" && e.EntityId == orderId);
    }

    /// <summary>
    /// Verifies that a static class event invoked from the client is executed on the server.
    /// </summary>
    [Fact]
    public async Task RemoteEvent_StaticClass_ClientToServer_ExecutesOnServer()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();

        // Get the event delegate from the client scope
        var notifyWarehouse = scopes.client.ServiceProvider
            .GetRequiredService<OrderEventHandler.NotifyWarehouseEvent>();

        // Get the server's test service to verify execution
        var serverProvider = scopes.client.ServiceProvider
            .GetRequiredService<ServerServiceProvider>()
            .serverProvider;
        var testService = serverProvider.GetRequiredService<IEventTestService>();
        testService.Clear();

        // Act
        var orderId = Guid.NewGuid();
        await notifyWarehouse(orderId);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(100);

        // Assert
        var recordedEvents = testService.GetRecordedEvents();
        Assert.Contains(recordedEvents, e => e.EventName == "NotifyWarehouse" && e.EntityId == orderId);
    }

    /// <summary>
    /// Verifies that event parameters are correctly serialized across the client-server boundary.
    /// </summary>
    [Fact]
    public async Task RemoteEvent_WithMultipleParameters_SerializesCorrectly()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();

        // Get the event delegate with multiple parameters
        var notifyShipped = scopes.client.ServiceProvider
            .GetRequiredService<OrderEventTarget.NotifyOrderShippedEvent>();

        // Get the server's test service
        var serverProvider = scopes.client.ServiceProvider
            .GetRequiredService<ServerServiceProvider>()
            .serverProvider;
        var testService = serverProvider.GetRequiredService<IEventTestService>();
        testService.Clear();

        // Act
        var orderId = Guid.NewGuid();
        var message = "TestMessage123";
        await notifyShipped(orderId, message);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(100);

        // Assert - verify both the orderId and message were passed correctly
        var recordedEvents = testService.GetRecordedEvents();
        Assert.Contains(recordedEvents, e =>
            e.EventName == $"NotifyShipped:{message}" && e.EntityId == orderId);
    }

    /// <summary>
    /// Verifies that fire-and-forget semantics work - the client can discard the task
    /// and the event still executes on the server.
    /// </summary>
    [Fact]
    public async Task RemoteEvent_FireAndForget_EventStillExecutes()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();

        var sendConfirmation = scopes.client.ServiceProvider
            .GetRequiredService<OrderEventTarget.SendOrderConfirmationEvent>();

        var serverProvider = scopes.client.ServiceProvider
            .GetRequiredService<ServerServiceProvider>()
            .serverProvider;
        var testService = serverProvider.GetRequiredService<IEventTestService>();
        testService.Clear();

        // Act - fire-and-forget (discard the task)
        var orderId = Guid.NewGuid();
        _ = sendConfirmation(orderId);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(200);

        // Assert
        var recordedEvents = testService.GetRecordedEvents();
        Assert.Contains(recordedEvents, e => e.EventName == "SendConfirmation" && e.EntityId == orderId);
    }

    /// <summary>
    /// Verifies that local (non-remote) events also work correctly with scope isolation.
    /// </summary>
    [Fact]
    public async Task LocalEvent_ScopeIsolation_ExecutesInNewScope()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();

        // Use the local scope (Logical mode - no remote call)
        var sendConfirmation = scopes.local.ServiceProvider
            .GetRequiredService<OrderEventTarget.SendOrderConfirmationEvent>();

        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();
        testService.Clear();

        // Act
        var orderId = Guid.NewGuid();
        await sendConfirmation(orderId);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(100);

        // Assert
        var recordedEvents = testService.GetRecordedEvents();
        Assert.Contains(recordedEvents, e => e.EventName == "SendConfirmation" && e.EntityId == orderId);
    }
}
