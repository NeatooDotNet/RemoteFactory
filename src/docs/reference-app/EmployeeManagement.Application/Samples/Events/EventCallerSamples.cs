using EmployeeManagement.Domain.Samples.Events;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Events;

#region events-caller
/// <summary>
/// Demonstrates how to invoke events from application code.
/// </summary>
public class EmployeeEventCaller
{
    private readonly EmployeeBasicEvent.SendWelcomeEmailEvent _sendWelcomeEmail;
    private readonly IEventTracker _eventTracker;

    public EmployeeEventCaller(
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail,
        IEventTracker eventTracker)
    {
        _sendWelcomeEmail = sendWelcomeEmail;
        _eventTracker = eventTracker;
    }

    /// <summary>
    /// Creates employee and fires welcome email event.
    /// </summary>
    public async Task OnboardEmployeeAsync(Guid employeeId, string email)
    {
        // Fire event - returns immediately without waiting
        // Code continues executing while event runs in background
        _ = _sendWelcomeEmail(employeeId, email);

        // Execution continues immediately - email sends asynchronously
        Console.WriteLine("Employee onboarded - welcome email queued");
    }

    /// <summary>
    /// For testing: wait for all pending events to complete.
    /// </summary>
    public async Task WaitForEventsAsync(CancellationToken ct)
    {
        // Use IEventTracker.WaitAllAsync() in tests to verify event side effects
        await _eventTracker.WaitAllAsync(ct);
    }
}
#endregion
