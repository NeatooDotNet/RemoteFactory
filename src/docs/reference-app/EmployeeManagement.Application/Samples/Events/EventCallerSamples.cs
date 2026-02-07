using EmployeeManagement.Domain.Samples.Events;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Events;

#region events-caller
// Fire event via generated delegate - returns immediately
public class EmployeeEventCaller
{
    private readonly EmployeeBasicEvent.SendWelcomeEmailEvent _sendWelcomeEmail;
    private readonly IEventTracker _eventTracker;

    public EmployeeEventCaller(EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail, IEventTracker eventTracker)
    {
        _sendWelcomeEmail = sendWelcomeEmail;
        _eventTracker = eventTracker;
    }

    public void OnboardEmployee(Guid employeeId, string email)
    {
        _ = _sendWelcomeEmail(employeeId, email); // Fire-and-forget
    }

    public async Task WaitForEventsAsync(CancellationToken ct) => await _eventTracker.WaitAllAsync(ct);
}
#endregion
