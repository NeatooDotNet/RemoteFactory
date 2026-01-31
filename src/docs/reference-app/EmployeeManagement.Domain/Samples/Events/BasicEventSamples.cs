using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-basic
/// <summary>
/// Employee aggregate demonstrating basic event pattern.
/// </summary>
[Factory]
public partial class EmployeeBasicEvent
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    [Create]
    public void Create(string firstName, string lastName, string email)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    /// <summary>
    /// Sends a welcome email asynchronously.
    /// Event executes in a new DI scope with fire-and-forget semantics.
    /// </summary>
    [Event]
    public async Task SendWelcomeEmail(
        Guid employeeId,
        string email,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            email,
            "Welcome to the Company!",
            $"Welcome! Your employee ID is {employeeId}.",
            ct);
    }
}
#endregion
