using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests for correlation ID propagation to [FactoryEventHandler] handler scopes.
/// </summary>
public class FactoryEventHandlerCorrelationTests
{
    private static (IServiceScope client, IServiceScope server, IServiceScope local) CreateScopes()
    {
        return ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: null);
    }

    [Fact]
    public async Task Raise_CorrelationId_PropagatedToHandlerScope()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();
        var correlationContext = server.GetRequiredService<ICorrelationContext>();

        // Set a correlation ID in the publisher's scope
        var expectedCorrelationId = "test-correlation-" + Guid.NewGuid();
        correlationContext.CorrelationId = expectedCorrelationId;

        var orderId = Guid.NewGuid();
        await events.Raise(new TestOrderEvent(orderId, "corr@test.com"));

        var recorded = testService.GetRecordedEventsWithCorrelation();
        var correlationEvent = recorded.FirstOrDefault(e => e.EventName == "CorrelationHandler" && e.EntityId == orderId);

        Assert.NotNull(correlationEvent.EventName);
        Assert.Equal(expectedCorrelationId, correlationEvent.CorrelationId);
    }

    [Fact]
    public async Task Raise_MultipleHandlers_AllGetSameCorrelationId()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();
        var correlationContext = server.GetRequiredService<ICorrelationContext>();

        var expectedCorrelationId = "multi-corr-" + Guid.NewGuid();
        correlationContext.CorrelationId = expectedCorrelationId;

        var orderId = Guid.NewGuid();
        await events.Raise(new TestOrderEvent(orderId, "multi-corr@test.com"));

        // The CorrelationHandler records correlationId — verify it matches
        var recorded = testService.GetRecordedEventsWithCorrelation();
        var correlationEvents = recorded.Where(e => e.EntityId == orderId && e.CorrelationId != null).ToList();

        Assert.NotEmpty(correlationEvents);
        Assert.All(correlationEvents, e => Assert.Equal(expectedCorrelationId, e.CorrelationId));
    }
}
