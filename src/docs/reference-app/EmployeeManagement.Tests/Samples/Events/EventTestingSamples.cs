using EmployeeManagement.Domain.Samples.Events;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Tests.Samples.Events;

#region events-eventtracker-wait
/// <summary>
/// Test demonstrating event side effect verification.
/// </summary>
public static class EventWaitTestSample
{
    public static async Task VerifyEventSideEffects(
        IEventTracker eventTracker,
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail)
    {
        // Clear any previous test data
        InMemoryEmailService.Clear();

        // Fire the event
        var employeeId = Guid.NewGuid();
        var email = "test@company.com";
        _ = sendWelcomeEmail(employeeId, email);

        // Wait for event to complete
        await eventTracker.WaitAllAsync();

        // Assert side effects via mock service
        var sentEmails = InMemoryEmailService.GetSentEmails();
        Assert.Single(sentEmails);
        Assert.Equal(email, sentEmails[0].Recipient);
        Assert.Contains("Welcome", sentEmails[0].Subject, StringComparison.Ordinal);
    }
}
#endregion

#region events-eventtracker-count
/// <summary>
/// Test demonstrating PendingCount monitoring.
/// </summary>
public static class EventCountTestSample
{
    public static async Task VerifyEventCounting(
        IEventTracker eventTracker,
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail)
    {
        // Assert initial state - no pending events
        Assert.Equal(0, eventTracker.PendingCount);

        // Fire multiple events
        _ = sendWelcomeEmail(Guid.NewGuid(), "emp1@company.com");
        _ = sendWelcomeEmail(Guid.NewGuid(), "emp2@company.com");

        // PendingCount may already be 0 if events complete quickly
        // This is expected for fast operations
        var pendingAfterFire = eventTracker.PendingCount;
        Console.WriteLine($"Pending after fire: {pendingAfterFire}");

        // Wait for completion
        await eventTracker.WaitAllAsync();

        // Verify all events completed
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
#endregion

#region events-testing
/// <summary>
/// Standard pattern for testing event side effects.
/// </summary>
public static class EventTestingPatternSample
{
    public static async Task TestWelcomeEmailEvent(
        IServiceProvider serviceProvider,
        IEventTracker eventTracker)
    {
        // Arrange - get event delegate from DI
        var sendWelcomeEmail = serviceProvider
            .GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();

        // Clear test data
        InMemoryEmailService.Clear();

        // Act - fire the event
        var employeeId = Guid.NewGuid();
        var testEmail = "newemployee@company.com";
        _ = sendWelcomeEmail(employeeId, testEmail);

        // Wait for event completion
        await eventTracker.WaitAllAsync();

        // Assert - verify email was sent
        var sentEmails = InMemoryEmailService.GetSentEmails();
        Assert.Single(sentEmails);
        Assert.Equal(testEmail, sentEmails[0].Recipient);
    }
}
#endregion

#region events-testing-latch
/// <summary>
/// Testing multiple concurrent events.
/// </summary>
public static class MultipleEventTestSample
{
    public static async Task TestMultipleConcurrentEvents(
        IServiceProvider serviceProvider,
        IEventTracker eventTracker)
    {
        // Arrange
        var sendWelcomeEmail = serviceProvider
            .GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();

        InMemoryEmailService.Clear();

        // Act - fire multiple events
        _ = sendWelcomeEmail(Guid.NewGuid(), "emp1@company.com");
        _ = sendWelcomeEmail(Guid.NewGuid(), "emp2@company.com");

        // Wait for all events using IEventTracker
        await eventTracker.WaitAllAsync();

        // Assert - verify all events completed
        var sentEmails = InMemoryEmailService.GetSentEmails();
        Assert.Equal(2, sentEmails.Count);
    }
}
#endregion
