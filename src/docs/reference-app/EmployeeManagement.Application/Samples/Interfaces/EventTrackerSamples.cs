using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Interfaces;

// Additional IEventTracker example - compiled but no longer extracted as duplicate
// Primary snippet is in InterfacesSamples.cs
public class GracefulShutdownDemo
{
    private readonly IEventTracker _eventTracker;

    public GracefulShutdownDemo(IEventTracker eventTracker)
    {
        _eventTracker = eventTracker;
    }

    public async Task ShutdownGracefullyAsync(CancellationToken ct)
    {
        var pendingCount = _eventTracker.PendingCount;
        Console.WriteLine($"Waiting for {pendingCount} pending events...");
        await _eventTracker.WaitAllAsync(ct);
        Console.WriteLine("All events completed. Safe to shutdown.");
    }
}
