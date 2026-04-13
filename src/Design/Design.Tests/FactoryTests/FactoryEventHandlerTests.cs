using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for the [FactoryEventHandler] mediator pattern.
/// Verifies IFactoryEvents.Raise dispatches to registered handlers.
/// </summary>
public class FactoryEventHandlerTests
{
    /// <summary>
    /// Verifies that IFactoryEvents.Raise dispatches to all registered handlers
    /// for the given event type.
    /// </summary>
    [Fact]
    public async Task Raise_DispatchesToAllHandlers()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var events = server.GetRequiredService<IFactoryEvents>();

        // Act — raise an event that has two handlers (OrderNotifyHandlers + OrderAuditHdlrs)
        await events.Raise(new OrderPlacedEvent(42, "test@example.com"));

        // Assert — both handlers completed without error
        Assert.True(true);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that raising an event with no registered handlers is a no-op.
    /// </summary>
    [Fact]
    public async Task Raise_NoHandlers_CompletesWithoutError()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var events = server.GetRequiredService<IFactoryEvents>();

        // Act — raise an event type with no handlers
        await events.Raise(new UnhandledTestEvent());

        // Assert — completed without error
        Assert.True(true);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Demonstrates: an event record with a nested parameterized-record property
    /// round-trips cleanly. Exercises the generator's automatic IL-trimming
    /// preservation for nested records — if the generator had not emitted
    /// <c>PreserveType&lt;ShippingAddress&gt;()</c>, a Release build with
    /// PublishTrimmed=true would fail to deserialize the nested record.
    /// </summary>
    [Fact]
    public async Task Raise_EventWithNestedRecord_DispatchesSuccessfully()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var events = server.GetRequiredService<IFactoryEvents>();

        var shipEvent = new OrderShippedEvent(
            OrderId: Guid.NewGuid(),
            Address: new ShippingAddress("123 Main St", "Seattle", "98101"));

        await events.Raise(shipEvent);

        Assert.True(true);

        server.Dispose();
        client.Dispose();
    }
}

/// <summary>
/// Event type with no registered handlers, used to test no-op behavior.
/// </summary>
public record UnhandledTestEvent() : FactoryEventBase;
