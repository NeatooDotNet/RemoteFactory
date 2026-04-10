using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests for [FactoryEventHandler] error handling and ContinueOnFail behavior.
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
    public async Task Raise_HandlerThrows_Default_ExceptionPropagates()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();

        var id = Guid.NewGuid();

        // Default options — exception should propagate
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => events.Raise(new TestFailingEvent(id, ShouldThrow: true)));
    }

    [Fact]
    public async Task Raise_HandlerThrows_ContinueOnFail_OtherHandlersStillRun()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var id = Guid.NewGuid();

        // ContinueOnFail — the survivor handler should still execute
        try
        {
            await events.Raise(new TestFailingEvent(id, ShouldThrow: true), RaiseOptions.ContinueOnFail);
        }
        catch
        {
            // Expected — one handler threw
        }

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        // SurvivorHandler should have run despite TestFailingHandler throwing
        Assert.Contains(recorded, e => e.EventName == "SurvivorHandler" && e.EntityId == id);
    }

    [Fact]
    public async Task Raise_NoHandlerThrows_ContinueOnFail_CompletesNormally()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        var id = Guid.NewGuid();

        // ShouldThrow: false — both handlers succeed
        await events.Raise(new TestFailingEvent(id, ShouldThrow: false), RaiseOptions.ContinueOnFail);

        await Task.Delay(200);

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "FailHandler" && e.EntityId == id);
        Assert.Contains(recorded, e => e.EventName == "SurvivorHandler" && e.EntityId == id);
    }

    [Fact]
    public async Task Raise_HandlerThrows_ContinueOnFail_ExceptionStillThrown()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();

        var id = Guid.NewGuid();

        // Even with ContinueOnFail, exceptions are thrown after all handlers complete.
        // With multiple handler registrations, they're aggregated.
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => events.Raise(new TestFailingEvent(id, ShouldThrow: true), RaiseOptions.ContinueOnFail));

        Assert.Contains(id.ToString(), ex.Message);
    }
}
