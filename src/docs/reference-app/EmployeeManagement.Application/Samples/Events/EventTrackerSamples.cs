using EmployeeManagement.Domain.Samples.Events;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Events;

// Full demo class - not a doc snippet (region removed to avoid duplicate)
public class EventTrackerAccessDemo
{
    private readonly IEventTracker _eventTracker;
    private readonly EmployeeBasicEvent.SendWelcomeEmailEvent _sendWelcomeEmail;

    public EventTrackerAccessDemo(IEventTracker eventTracker, EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail)
    {
        _eventTracker = eventTracker;
        _sendWelcomeEmail = sendWelcomeEmail;
    }

    public async Task DemonstrateEventTrackerAsync()
    {
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee1@company.com");
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee2@company.com");
        await _eventTracker.WaitAllAsync();
    }
}
