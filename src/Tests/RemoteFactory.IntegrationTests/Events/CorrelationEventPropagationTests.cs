#pragma warning disable xUnit1051 // Test timing delays don't need test cancellation token

using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events;

/// <summary>
/// Integration tests for correlation ID propagation to event handlers.
/// These tests verify that correlation IDs set in the parent scope are
/// correctly captured and restored in event handler scopes.
/// Uses isolated containers per test to avoid race conditions with the
/// shared singleton IEventTestService in cached containers.
/// </summary>
public class CorrelationEventPropagationTests
{
    /// <summary>
    /// Creates isolated scopes with per-test container instances.
    /// This avoids sharing the singleton IEventTestService with other tests.
    /// </summary>
    private static (IServiceScope client, IServiceScope server, IServiceScope local) CreateIsolatedScopes()
    {
        // Using the custom Scopes() overload creates completely fresh containers
        // Each call gets its own IEventTestService singleton (not shared with cached containers)
        return ClientServerContainers.Scopes(configureClient: null, configureServer: null);
    }

    /// <summary>
    /// Verifies that correlation ID is propagated from parent scope to event handler.
    /// </summary>
    [Fact]
    public async Task Event_PropagatesCorrelationId_FromParentScope()
    {
        // Arrange
        var scopes = CreateIsolatedScopes();

        // Set correlation ID on the local scope BEFORE resolving the event delegate
        var localCorrelation = scopes.local.ServiceProvider.GetRequiredService<ICorrelationContext>();
        var expectedCorrelationId = "test-corr-001";
        localCorrelation.CorrelationId = expectedCorrelationId;

        // Get the event delegate - correlation context is captured at this point
        var processEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationEvent>();

        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act
        var entityId = Guid.NewGuid();
        await processEvent(entityId);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(100);

        // Assert - unique entityId prevents collision with other tests
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EventName == "ProcessWithCorrelation" &&
            e.EntityId == entityId &&
            e.CorrelationId == expectedCorrelationId);
    }

    /// <summary>
    /// Verifies that static class events also receive correlation IDs.
    /// </summary>
    [Fact]
    public async Task StaticEvent_PropagatesCorrelationId_FromParentScope()
    {
        // Arrange
        var scopes = CreateIsolatedScopes();

        // Set correlation ID BEFORE resolving the event delegate
        var localCorrelation = scopes.local.ServiceProvider.GetRequiredService<ICorrelationContext>();
        var expectedCorrelationId = "static-corr-002";
        localCorrelation.CorrelationId = expectedCorrelationId;

        // Get the event delegate - correlation context is captured at this point
        var handleEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventHandler.HandleWithCorrelationEvent>();

        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act
        var entityId = Guid.NewGuid();
        await handleEvent(entityId);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(100);

        // Assert - unique entityId prevents collision with other tests
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EventName == "StaticHandleWithCorrelation" &&
            e.EntityId == entityId &&
            e.CorrelationId == expectedCorrelationId);
    }

    /// <summary>
    /// Verifies that correlation ID is captured at invocation time, not resolution time.
    /// This is the correct behavior for fire-and-forget events - you want the correlation
    /// ID that was active when the event was fired.
    /// </summary>
    [Fact]
    public async Task Events_CaptureCorrelationAtInvocationTime()
    {
        // Arrange - use isolated scopes to avoid interference
        var scopes = CreateIsolatedScopes();

        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();
        var correlationContext = scopes.local.ServiceProvider.GetRequiredService<ICorrelationContext>();

        // Set initial correlation and resolve event delegate
        correlationContext.CorrelationId = "initial-correlation";
        var fireEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationEvent>();

        // Change correlation ID BEFORE invoking
        correlationContext.CorrelationId = "current-correlation";

        // Act - fire event with current correlation
        var entityId = Guid.NewGuid();
        await fireEvent(entityId);

        await Task.Delay(100);

        // Assert - event captured the correlation ID from invocation time
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();

        Assert.Contains(recordedEvents, e =>
            e.EntityId == entityId &&
            e.CorrelationId == "current-correlation");
    }

    /// <summary>
    /// Verifies that event parameters are correctly passed alongside correlation ID.
    /// </summary>
    [Fact]
    public async Task Event_WithMessageParameter_PropagatesCorrelationId()
    {
        // Arrange
        var scopes = CreateIsolatedScopes();

        var localCorrelation = scopes.local.ServiceProvider.GetRequiredService<ICorrelationContext>();
        var expectedCorrelationId = "msg-corr-003";
        localCorrelation.CorrelationId = expectedCorrelationId;

        var processEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationAndMessageEvent>();

        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act
        var entityId = Guid.NewGuid();
        var message = "TestMessage";
        await processEvent(entityId, message);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(100);

        // Assert - unique entityId prevents collision with other tests
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EventName == $"ProcessWithMessage:{message}" &&
            e.EntityId == entityId &&
            e.CorrelationId == expectedCorrelationId);
    }

    /// <summary>
    /// Verifies that fire-and-forget semantics still work with correlation propagation.
    /// </summary>
    [Fact]
    public async Task Event_FireAndForget_StillPropagatesCorrelationId()
    {
        // Arrange
        var scopes = CreateIsolatedScopes();

        // Set correlation ID BEFORE resolving the event delegate
        var localCorrelation = scopes.local.ServiceProvider.GetRequiredService<ICorrelationContext>();
        var expectedCorrelationId = "fire-forget-004";
        localCorrelation.CorrelationId = expectedCorrelationId;

        var processEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationEvent>();

        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act - fire-and-forget (discard the task)
        var entityId = Guid.NewGuid();
        _ = processEvent(entityId);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(200);

        // Assert - unique entityId prevents collision with other tests
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EventName == "ProcessWithCorrelation" &&
            e.EntityId == entityId &&
            e.CorrelationId == expectedCorrelationId);
    }

    /// <summary>
    /// Verifies that when correlation ID is not set, event still executes (with null correlation).
    /// </summary>
    [Fact]
    public async Task Event_WithoutCorrelationId_StillExecutes()
    {
        // Arrange
        var scopes = CreateIsolatedScopes();

        // Do not set correlation ID - leave it as null
        var processEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationEvent>();

        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act
        var entityId = Guid.NewGuid();
        await processEvent(entityId);

        // Allow time for the fire-and-forget task to complete
        await Task.Delay(100);

        // Assert - event executed, correlation may be null or auto-generated
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EventName == "ProcessWithCorrelation" &&
            e.EntityId == entityId);
    }

    /// <summary>
    /// Verifies that changing correlation ID after capturing does not affect the event.
    /// The correlation ID should be captured at the point of event invocation.
    /// </summary>
    [Fact]
    public async Task Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges()
    {
        // Arrange
        var scopes = CreateIsolatedScopes();

        var localCorrelation = scopes.local.ServiceProvider.GetRequiredService<ICorrelationContext>();
        var originalCorrelationId = "original-005";
        localCorrelation.CorrelationId = originalCorrelationId;

        var processEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationEvent>();

        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act - fire event, then immediately change correlation ID
        var entityId = Guid.NewGuid();
        var eventTask = processEvent(entityId);

        // Change the correlation ID after firing the event
        localCorrelation.CorrelationId = "changed-after-fire";

        await eventTask;
        await Task.Delay(100);

        // Assert - event should have the original correlation ID
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EventName == "ProcessWithCorrelation" &&
            e.EntityId == entityId &&
            e.CorrelationId == originalCorrelationId);
    }
}
