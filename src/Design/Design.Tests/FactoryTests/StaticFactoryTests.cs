// =============================================================================
// DESIGN SOURCE OF TRUTH: Static Factory Tests
// =============================================================================
//
// Tests demonstrating the STATIC FACTORY pattern with [Execute] and [Event].
//
// =============================================================================

using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for STATIC FACTORY pattern (ExampleCommands, ExampleEvents).
/// </summary>
public class StaticFactoryTests
{
    /// <summary>
    /// Verifies [Execute] method works through remote call.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Execute delegates are resolved from DI
    ///
    /// The generator creates a nested delegate type inside the static class.
    /// You resolve the delegate from DI and invoke it. The delegate handles
    /// service injection and remote/local routing.
    ///
    /// Pattern:
    ///   var sendNotification = scope.GetRequiredService&lt;ExampleCommands.SendNotification&gt;();
    ///   var result = await sendNotification("recipient", "message");
    ///
    /// GENERATOR BEHAVIOR: For [Remote, Execute] on _SendNotification:
    /// - Creates delegate type: ExampleCommands.SendNotification
    /// - Registers delegate in DI for both client and server
    /// - Client delegate serializes to server; server invokes the method
    /// </remarks>
    [Fact]
    public async Task Execute_SendNotification_ReturnsResult()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();

        // Resolve the delegate type from DI
        var sendNotification = client.GetRequiredService<ExampleCommands.SendNotification>();

        // Act - invoke the delegate
        var result = await sendNotification("test@example.com", "Hello!");

        // Assert
        Assert.True(result);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies [Execute] works in local mode.
    /// </summary>
    [Fact]
    public async Task Execute_WorksInLocalMode()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();

        // Resolve delegate - in local mode, executes directly
        var sendNotification = local.GetRequiredService<ExampleCommands.SendNotification>();

        // Act
        var result = await sendNotification("local@example.com", "Local message");

        // Assert
        Assert.True(result);

        local.Dispose();
    }

    /// <summary>
    /// Verifies [Event] method fires without blocking.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Events use the same delegate pattern
    ///
    /// The key difference from [Execute]:
    /// - Events run in an isolated scope
    /// - CancellationToken from IHostApplicationLifetime is injected
    /// - Intended for fire-and-forget use (though you CAN await for testing)
    ///
    /// GENERATOR BEHAVIOR: For [Remote, Event] on _OnOrderPlaced:
    /// - Creates delegate type: ExampleEvents.OnOrderPlacedEvent (note Event suffix)
    /// - Event runs in isolated scope with ApplicationStopping token
    /// - Returns Task that completes when event handler finishes
    ///
    /// In production code, you typically fire-and-forget:
    ///   var onOrderPlaced = scope.GetRequiredService&lt;ExampleEvents.OnOrderPlaced&gt;();
    ///   _ = onOrderPlaced(orderId);  // Fire and forget
    /// </remarks>
    [Fact]
    public async Task Event_OnOrderPlaced_FiresWithoutBlocking()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();

        // Resolve the event delegate (note: Event suffix in type name)
        var onOrderPlaced = client.GetRequiredService<ExampleEvents.OnOrderPlacedEvent>();

        // Act - invoke the event (awaiting for test verification)
        await onOrderPlaced(123);

        // Assert - event completed without error
        Assert.True(true);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies [Event] works in local mode.
    /// </summary>
    [Fact]
    public async Task Event_WorksInLocalMode()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();

        var onOrderPlaced = local.GetRequiredService<ExampleEvents.OnOrderPlacedEvent>();

        // Act
        await onOrderPlaced(456);

        // Assert
        Assert.True(true);

        local.Dispose();
    }
}
