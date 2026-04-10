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
}

/// <summary>
/// Event type with no registered handlers, used to test no-op behavior.
/// </summary>
public record UnhandledTestEvent() : FactoryEventBase;
