using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests for [FactoryEventHandler] error handling.
/// Handlers run sequentially in the caller's scope; the first exception aborts the chain
/// and propagates to the caller so the caller's transaction can roll back.
/// </summary>
public class FactoryEventHandlerErrorTests
{
    private static (IServiceScope client, IServiceScope server, IServiceScope local) CreateScopes()
    {
        return ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: null);
    }

    [Fact]
    public async Task Raise_HandlerThrows_ExceptionPropagates()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();

        var id = Guid.NewGuid();

        // A throwing handler aborts the chain and the exception reaches the caller.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => events.Raise(new TestFailingEvent(id, ShouldThrow: true)));
    }

    [Fact]
    public async Task Raise_NoHandlerThrows_CompletesNormally()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var id = Guid.NewGuid();

        // ShouldThrow: false — every handler succeeds and both record their event.
        await events.Raise(new TestFailingEvent(id, ShouldThrow: false));

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "FailHandler" && e.EntityId == id);
        Assert.Contains(recorded, e => e.EventName == "SurvivorHandler" && e.EntityId == id);
    }
}
