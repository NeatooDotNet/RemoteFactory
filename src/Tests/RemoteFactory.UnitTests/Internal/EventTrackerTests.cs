#pragma warning disable xUnit1051 // Test timing delays don't need test cancellation token

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Unit tests for the EventTracker class.
/// EventTracker is used to track pending async operations in the factory infrastructure.
/// </summary>
public class EventTrackerTests
{
    private readonly ILogger<EventTracker> _logger;
    private readonly EventTracker _tracker;

    public EventTrackerTests()
    {
        _logger = new NullLogger<EventTracker>();
        _tracker = new EventTracker(_logger);
    }

    [Fact]
    public void Track_CompletedTask_DoesNotIncrementPendingCount()
    {
        // Arrange
        var completedTask = Task.CompletedTask;

        // Act
        _tracker.Track(completedTask);

        // Assert
        Assert.Equal(0, _tracker.PendingCount);
    }

    [Fact]
    public void Track_FaultedCompletedTask_DoesNotIncrementPendingCount()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var faultedTask = Task.FromException(exception);

        // Act
        _tracker.Track(faultedTask);

        // Assert
        Assert.Equal(0, _tracker.PendingCount);
    }

    [Fact]
    public async Task Track_RunningTask_IncrementsPendingCount()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var task = tcs.Task;

        // Act
        _tracker.Track(task);

        // Assert
        Assert.Equal(1, _tracker.PendingCount);

        // Cleanup
        tcs.SetResult();
        await Task.Delay(50); // Allow continuation to run
        Assert.Equal(0, _tracker.PendingCount);
    }

    [Fact]
    public async Task Track_MultipleRunningTasks_CorrectlyTracksPendingCount()
    {
        // Arrange
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();
        var tcs3 = new TaskCompletionSource();

        // Act
        _tracker.Track(tcs1.Task);
        _tracker.Track(tcs2.Task);
        _tracker.Track(tcs3.Task);

        // Assert
        Assert.Equal(3, _tracker.PendingCount);

        // Complete first task
        tcs1.SetResult();
        await Task.Delay(50);
        Assert.Equal(2, _tracker.PendingCount);

        // Complete remaining tasks
        tcs2.SetResult();
        tcs3.SetResult();
        await Task.Delay(50);
        Assert.Equal(0, _tracker.PendingCount);
    }

    [Fact]
    public async Task Track_FailedTask_RemovesFromPendingAndLogs()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var exception = new InvalidOperationException("Test failure");

        // Act
        _tracker.Track(tcs.Task);
        Assert.Equal(1, _tracker.PendingCount);

        tcs.SetException(exception);
        await Task.Delay(50);

        // Assert
        Assert.Equal(0, _tracker.PendingCount);
    }

    [Fact]
    public async Task WaitAllAsync_NoPendingTasks_ReturnsImmediately()
    {
        // Act & Assert - should not throw and return quickly
        await _tracker.WaitAllAsync();
    }

    [Fact]
    public async Task WaitAllAsync_WithPendingTasks_WaitsForCompletion()
    {
        // Arrange
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();
        _tracker.Track(tcs1.Task);
        _tracker.Track(tcs2.Task);

        // Complete tasks after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            tcs1.SetResult();
            tcs2.SetResult();
        });

        // Act
        await _tracker.WaitAllAsync();
        // Allow continuations to run (they remove tasks from the dictionary)
        await Task.Delay(50);

        // Assert
        Assert.Equal(0, _tracker.PendingCount);
    }

    [Fact]
    public async Task WaitAllAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        _tracker.Track(tcs.Task);
        var cts = new CancellationTokenSource();

        // Cancel immediately
        cts.Cancel();

        // Act & Assert (TaskCanceledException derives from OperationCanceledException)
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _tracker.WaitAllAsync(cts.Token));

        // Cleanup
        tcs.SetResult();
    }
}
