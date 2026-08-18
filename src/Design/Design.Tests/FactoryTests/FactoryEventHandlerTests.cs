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
    /// Records what the handlers actually did. The Design handlers all send through
    /// <see cref="INotificationService"/>, so substituting a recording implementation
    /// makes dispatch observable without changing the demonstration in
    /// FactoryEventHandlerPattern.cs.
    /// </summary>
    private sealed class RecordingNotificationService : INotificationService
    {
        private readonly List<(string Recipient, string Message)> _sent = [];

        public IReadOnlyList<(string Recipient, string Message)> Sent
        {
            get
            {
                lock (_sent)
                {
                    return [.. _sent];
                }
            }
        }

        public Task SendAsync(string recipient, string message)
        {
            lock (_sent)
            {
                _sent.Add((recipient, message));
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Server scopes whose INotificationService records instead of simulating. The
    /// configure callback runs after the base registrations, so this registration is
    /// the one resolved.
    /// </summary>
    private static (IServiceScope server, IServiceScope client, RecordingNotificationService notifications) ScopesWithRecordedNotifications()
    {
        var notifications = new RecordingNotificationService();
        var (server, client, _) = DesignClientServerContainers.Scopes(
            configureServer: services => services.AddScoped<INotificationService>(_ => notifications));
        return (server, client, notifications);
    }

    /// <summary>
    /// Verifies that IFactoryEvents.Raise dispatches to all registered handlers
    /// for the given event type.
    /// </summary>
    /// <remarks>
    /// "All" is the load-bearing word, so both handlers are named in the assertion:
    /// OrderNotifyHandlers mails the customer, OrderAuditHdlrs mails the audit box.
    /// A dispatcher that stopped after the first registration — or a registry whose
    /// (event type, handler class) dedupe collapsed the two — leaves one of these
    /// missing.
    /// </remarks>
    [Fact]
    public async Task Raise_DispatchesToAllHandlers()
    {
        // Arrange
        var (server, client, notifications) = ScopesWithRecordedNotifications();
        var events = server.GetRequiredService<IFactoryEvents>();

        // Act — raise an event that has two handlers (OrderNotifyHandlers + OrderAuditHdlrs)
        await events.Raise(new OrderPlacedEvent(42, "test@example.com"));

        // Assert — both handlers ran. Order between them is unspecified by contract,
        // so this asserts membership rather than sequence.
        var sent = notifications.Sent;
        Assert.Equal(2, sent.Count);
        Assert.Contains(sent, s => s.Recipient == "test@example.com" && s.Message == "Order 42 confirmed!");
        Assert.Contains(sent, s => s.Recipient == "audit@example.com" && s.Message == "Audit: Order 42 placed by test@example.com");

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that raising an event with no registered handlers is a no-op.
    /// </summary>
    /// <remarks>
    /// The no-op claim needs an observation of nothing happening, not just the absence
    /// of an exception: a routing bug that dispatched an unhandled event to some other
    /// event's handlers would complete without error too.
    /// </remarks>
    [Fact]
    public async Task Raise_NoHandlers_CompletesWithoutError()
    {
        // Arrange
        var (server, client, notifications) = ScopesWithRecordedNotifications();
        var events = server.GetRequiredService<IFactoryEvents>();

        // Act — raise an event type with no handlers
        await events.Raise(new UnhandledTestEvent());

        // Assert — nothing dispatched
        Assert.Empty(notifications.Sent);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Demonstrates: an event record with a nested parameterized-record property
    /// round-trips cleanly. The trimming preservation for this shape comes from the
    /// generated per-assembly event-preservation registrar, which walks each
    /// declared event's property graph and emits
    /// <c>PreserveType&lt;ShippingAddress&gt;()</c>; without it, a Release build with
    /// PublishTrimmed=true would fail to deserialize the nested record (this test
    /// runs untrimmed — the trimmed pin lives in RemoteFactory.TrimmingTests).
    /// </summary>
    [Fact]
    public async Task Raise_EventWithNestedRecord_DispatchesSuccessfully()
    {
        var (server, client, notifications) = ScopesWithRecordedNotifications();
        var events = server.GetRequiredService<IFactoryEvents>();

        var orderId = Guid.NewGuid();
        var shipEvent = new OrderShippedEvent(
            OrderId: orderId,
            Address: new ShippingAddress("123 Main St", "Seattle", "98101"));

        await events.Raise(shipEvent);

        // The handler reads through the nested record to build its message, so this
        // asserts the nested property graph arrived intact rather than merely that the
        // dispatch did not throw.
        var sent = Assert.Single(notifications.Sent);
        Assert.Equal("ops@example.com", sent.Recipient);
        Assert.Equal($"Order {orderId} shipped to Seattle", sent.Message);

        server.Dispose();
        client.Dispose();
    }
}

/// <summary>
/// Event type with no registered handlers, used to test no-op behavior.
/// </summary>
public record UnhandledTestEvent() : FactoryEventBase;
