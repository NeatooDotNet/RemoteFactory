namespace EmployeeManagement.Domain.Samples.Events;

#region events-tracker-generated
// Generated Event Delegate Pattern
//
// For an [Event] method like:
//   public async Task SendWelcomeEmail(Guid employeeId, string email,
//       [Service] IEmailService emailService, CancellationToken ct)
//
// The source generator produces:
//
// 1. Delegate type:
//    public delegate Task SendWelcomeEmailEvent(Guid employeeId, string email);
//
// 2. Factory implementation (simplified):
//    public Task SendWelcomeEmailDelegate(Guid employeeId, string email)
//    {
//        var task = Task.Run(async () =>
//        {
//            // Create new DI scope for isolation
//            using var scope = _serviceProvider.CreateScope();
//
//            // Resolve entity and services from new scope
//            var entity = scope.ServiceProvider.GetRequiredService<Employee>();
//            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
//
//            // Get cancellation token from host lifetime
//            var ct = _hostApplicationLifetime.ApplicationStopping;
//
//            // Execute the event method
//            await entity.SendWelcomeEmail(employeeId, email, emailService, ct);
//        });
//
//        // Track the task for graceful shutdown
//        _eventTracker.Track(task);
//
//        // Return task for optional awaiting (fire-and-forget callers ignore it)
//        return task;
//    }
//
// Key points:
// - Each event gets its own DI scope
// - CancellationToken comes from ApplicationStopping
// - EventTracker monitors the task
// - Caller receives the task but typically ignores it (_ = ...)
#endregion

/// <summary>
/// Placeholder class to hold the comment block.
/// </summary>
public static class GeneratedEventCodeIllustration
{
}
