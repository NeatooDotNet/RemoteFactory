using EmployeeManagement.Domain.Samples.Events;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Events;

#region events-eventtracker-access
/// <summary>
/// Demonstrates IEventTracker singleton access and basic usage.
/// </summary>
public class EventTrackerAccessDemo
{
    private readonly IEventTracker _eventTracker;
    private readonly EmployeeBasicEvent.SendWelcomeEmailEvent _sendWelcomeEmail;

    public EventTrackerAccessDemo(
        IEventTracker eventTracker,
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail)
    {
        // IEventTracker is a singleton registered by AddNeatooAspNetCore
        _eventTracker = eventTracker;
        _sendWelcomeEmail = sendWelcomeEmail;
    }

    public async Task DemonstrateEventTrackerAsync()
    {
        // Fire multiple events
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee1@company.com");
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee2@company.com");
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee3@company.com");

        // Check pending count (may be 0 if events complete quickly)
        var pendingCount = _eventTracker.PendingCount;
        Console.WriteLine($"Pending events: {pendingCount}");

        // Wait for all events to complete
        await _eventTracker.WaitAllAsync();

        // Verify all events completed
        if (_eventTracker.PendingCount != 0)
        {
            throw new InvalidOperationException("Expected no pending events");
        }
    }
}
#endregion
