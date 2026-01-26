namespace EmployeeManagement.Domain.Interfaces;

/// <summary>
/// Service for sending email notifications.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default);
}
