using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Unit tests for NullEventTracker, the no-op IEventTracker
/// used on Remote-mode clients where events serialize to the server.
/// </summary>
public class NullEventTrackerTests
{
    private readonly NullEventTracker _tracker = new();

    [Fact]
    public void PendingCount_ReturnsZero()
    {
        // Assert
        Assert.Equal(0, _tracker.PendingCount);
    }

    [Fact]
    public void Track_IsNoOp_DoesNotThrow()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        // Act -- should not throw or track the task
        _tracker.Track(tcs.Task);

        // Assert -- PendingCount remains 0
        Assert.Equal(0, _tracker.PendingCount);

        // Cleanup
        tcs.SetResult();
    }

    [Fact]
    public async Task WaitAllAsync_ReturnsCompletedTask()
    {
        // Act
        var result = _tracker.WaitAllAsync();

        // Assert -- should return immediately (Task.CompletedTask)
        Assert.True(result.IsCompleted);
        await result; // Should not throw
    }

    [Fact]
    public async Task WaitAllAsync_WithCancellationToken_ReturnsCompletedTask()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = _tracker.WaitAllAsync(cts.Token);

        // Assert -- should return immediately regardless of cancellation token
        Assert.True(result.IsCompleted);
        await result;
    }

    [Fact]
    public void Track_CompletedTask_DoesNotThrow()
    {
        // Act -- tracking a completed task should be a no-op
        _tracker.Track(Task.CompletedTask);

        // Assert
        Assert.Equal(0, _tracker.PendingCount);
    }

    [Fact]
    public void Track_FaultedTask_DoesNotThrow()
    {
        // Arrange
        var faultedTask = Task.FromException(new InvalidOperationException("test"));

        // Act -- tracking a faulted task should be a no-op (no logging, no throw)
        _tracker.Track(faultedTask);

        // Assert
        Assert.Equal(0, _tracker.PendingCount);
    }
}
