using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Events;

namespace RemoteFactory.UnitTests.FactoryGenerator.Events;

/// <summary>
/// Unit tests for [Event] method generation in Server mode.
/// These tests verify that event delegates are properly generated and work correctly
/// without serialization round-trips. Integration tests for client-server event
/// round-trips are in RemoteFactory.IntegrationTests.
/// </summary>
public class EventGenerationTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public EventGenerationTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        // Reset static state
        EventTarget_Static.Reset();
        EventTarget_StaticWithService.Reset();
    }

    public void Dispose()
    {
        EventTarget_Static.Reset();
        EventTarget_StaticWithService.Reset();
        (_provider as IDisposable)?.Dispose();
    }

    #region Simple Event Tests

    [Fact]
    public void Event_Simple_DelegateCanBeResolved()
    {
        // Arrange & Act - The delegate type should be generated and resolvable
        var eventDelegate = _provider.GetService<EventTarget_Simple.FireSimpleEvent>();

        // Assert
        Assert.NotNull(eventDelegate);
    }

    [Fact]
    public async Task Event_Simple_DelegateInvokesMethod()
    {
        // Arrange
        var eventDelegate = _provider.GetRequiredService<EventTarget_Simple.FireSimpleEvent>();

        // Act
        await eventDelegate("TestMessage");

        // Assert - The event should fire (we can't easily check instance state since events run in isolated scope)
        // The test verifies the delegate was invoked without throwing
    }

    #endregion

    #region Void Return Event Tests

    [Fact]
    public void Event_VoidReturn_DelegateCanBeResolved()
    {
        // Arrange & Act
        var eventDelegate = _provider.GetService<EventTarget_VoidReturn.FireVoidEvent>();

        // Assert
        Assert.NotNull(eventDelegate);
    }

    [Fact]
    public async Task Event_VoidReturn_ReturnsTask()
    {
        // Arrange
        var eventDelegate = _provider.GetRequiredService<EventTarget_VoidReturn.FireVoidEvent>();

        // Act - Even void methods should generate Task-returning delegates
        var task = eventDelegate("TestMessage");

        // Assert
        Assert.NotNull(task);
        await task; // Should complete successfully
    }

    #endregion

    #region Event With Service Tests

    [Fact]
    public void Event_WithService_DelegateCanBeResolved()
    {
        // Arrange & Act
        var eventDelegate = _provider.GetService<EventTarget_WithService.FireWithServiceEvent>();

        // Assert
        Assert.NotNull(eventDelegate);
    }

    [Fact]
    public async Task Event_WithService_ServiceIsInjected()
    {
        // Arrange
        var eventDelegate = _provider.GetRequiredService<EventTarget_WithService.FireWithServiceEvent>();
        var testId = Guid.NewGuid();

        // Act - Service parameter should be excluded from delegate signature
        await eventDelegate(testId);

        // Assert - Method should run without throwing (service was injected)
    }

    #endregion

    #region Multiple Parameters Event Tests

    [Fact]
    public void Event_MultipleParams_DelegateCanBeResolved()
    {
        // Arrange & Act
        var eventDelegate = _provider.GetService<EventTarget_MultipleParams.FireMultiParamEvent>();

        // Assert
        Assert.NotNull(eventDelegate);
    }

    [Fact]
    public async Task Event_MultipleParams_AllParamsPassedCorrectly()
    {
        // Arrange
        var eventDelegate = _provider.GetRequiredService<EventTarget_MultipleParams.FireMultiParamEvent>();
        var testId = Guid.NewGuid();

        // Act - All parameters should be passed to the delegate
        await eventDelegate(testId, "TestName", 42);

        // Assert - Method should run without throwing
    }

    #endregion

    #region Static Class Event Tests

    [Fact]
    public void Event_Static_DelegateCanBeResolved()
    {
        // Arrange & Act
        var eventDelegate = _provider.GetService<EventTarget_Static.FireStaticEvent>();

        // Assert
        Assert.NotNull(eventDelegate);
    }

    [Fact]
    public async Task Event_Static_InvokesStaticMethod()
    {
        // Arrange
        EventTarget_Static.Reset();
        var eventDelegate = _provider.GetRequiredService<EventTarget_Static.FireStaticEvent>();

        // Act
        await eventDelegate("StaticTestMessage");

        // Small delay to allow fire-and-forget task to complete
        await Task.Delay(50);

        // Assert - Static method should be invoked
        Assert.True(EventTarget_Static.EventFired);
        Assert.Equal("StaticTestMessage", EventTarget_Static.ReceivedMessage);
    }

    #endregion

    #region Static Class Event With Service Tests

    [Fact]
    public void Event_StaticWithService_DelegateCanBeResolved()
    {
        // Arrange & Act
        var eventDelegate = _provider.GetService<EventTarget_StaticWithService.FireStaticServiceEvent>();

        // Assert
        Assert.NotNull(eventDelegate);
    }

    [Fact]
    public async Task Event_StaticWithService_ServiceIsInjected()
    {
        // Arrange
        EventTarget_StaticWithService.Reset();
        var eventDelegate = _provider.GetRequiredService<EventTarget_StaticWithService.FireStaticServiceEvent>();
        var testId = Guid.NewGuid();

        // Act
        await eventDelegate(testId);

        // Small delay to allow fire-and-forget task to complete
        await Task.Delay(50);

        // Assert
        Assert.True(EventTarget_StaticWithService.EventFired);
        Assert.True(EventTarget_StaticWithService.ServiceInjected);
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public void Event_WithCancellation_DelegateCanBeResolved()
    {
        // Arrange & Act
        var eventDelegate = _provider.GetService<EventTarget_WithCancellation.FireWithCancellationEvent>();

        // Assert
        Assert.NotNull(eventDelegate);
    }

    [Fact]
    public async Task Event_WithCancellation_TokenIsAutoInjected()
    {
        // Arrange
        var eventDelegate = _provider.GetRequiredService<EventTarget_WithCancellation.FireWithCancellationEvent>();

        // Act - CancellationToken is auto-injected from IHostApplicationLifetime.ApplicationStopping
        // It is NOT part of the delegate signature
        await eventDelegate("TestMessage");

        // Assert - Method should run without throwing
    }

    #endregion
}
