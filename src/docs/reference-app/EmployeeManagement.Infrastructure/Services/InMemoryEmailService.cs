using EmployeeManagement.Domain.Interfaces;
using System.Collections.Concurrent;

namespace EmployeeManagement.Infrastructure.Services;

/// <summary>
/// In-memory implementation of IEmailService for demonstration.
/// Stores emails in memory rather than sending them.
/// </summary>
public class InMemoryEmailService : IEmailService
{
    private static readonly ConcurrentBag<EmailRecord> SentEmails = new();

    public Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        SentEmails.Add(new EmailRecord
        {
            Recipient = recipient,
            Subject = subject,
            Body = body,
            SentAt = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all sent emails. Useful for testing.
    /// </summary>
    public static IReadOnlyList<EmailRecord> GetSentEmails()
    {
        return SentEmails.ToList();
    }

    /// <summary>
    /// Clears all sent emails. Useful for testing.
    /// </summary>
    public static void Clear()
    {
        SentEmails.Clear();
    }
}

/// <summary>
/// Record of a sent email.
/// </summary>
public class EmailRecord
{
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; }
}
