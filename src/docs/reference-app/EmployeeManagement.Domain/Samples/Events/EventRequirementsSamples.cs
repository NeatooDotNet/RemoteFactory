using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-requirements
/// <summary>
/// Demonstrates valid event method signatures.
/// </summary>
[Factory]
public partial class EmployeeEventSignatures
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Returns Task with service injection - standard pattern.
    /// </summary>
    [Event]
    public async Task ValidEvent(
        Guid employeeId,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "notifications@company.com",
            "Event Notification",
            $"Event triggered for {employeeId}",
            ct);
    }

    /// <summary>
    /// Returns void - generator converts to Task delegate.
    /// </summary>
    [Event]
    public void VoidEvent(
        Guid employeeId,
        string message,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Sync operation completed
        Console.WriteLine($"Void event for {employeeId}: {message}");
    }

    /// <summary>
    /// Async Task with explicit await.
    /// </summary>
    [Event]
    public async Task AsyncEvent(
        Guid employeeId,
        int delayMs,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await Task.Delay(delayMs, ct);
        await auditLog.LogAsync(
            "AsyncEvent",
            employeeId,
            "Employee",
            $"Completed after {delayMs}ms delay",
            ct);
    }
}
#endregion
