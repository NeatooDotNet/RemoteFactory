using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Interfaces;

#region interfaces-eventtracker
/// <summary>
/// Demonstrates IEventTracker usage for graceful shutdown.
/// </summary>
public class GracefulShutdownDemo
{
    private readonly IEventTracker _eventTracker;

    public GracefulShutdownDemo(IEventTracker eventTracker)
    {
        _eventTracker = eventTracker;
    }

    /// <summary>
    /// Waits for all pending fire-and-forget events before shutdown.
    /// </summary>
    public async Task ShutdownGracefullyAsync(CancellationToken ct)
    {
        // IEventTracker monitors pending fire-and-forget events

        // Check PendingCount to see how many fire-and-forget events are in progress
        var pendingCount = _eventTracker.PendingCount;
        Console.WriteLine($"Waiting for {pendingCount} pending events to complete...");

        // Wait for all pending events to complete
        await _eventTracker.WaitAllAsync(ct);

        // Assert PendingCount equals 0 after WaitAllAsync completes
        if (_eventTracker.PendingCount != 0)
            throw new InvalidOperationException("Expected no pending events after WaitAllAsync");

        Console.WriteLine("All events completed. Safe to shutdown.");
    }
}
#endregion
