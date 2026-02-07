using EmployeeManagement.Domain.Samples.Events;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Tests.Samples.Events;

#region events-testing
// Fire event delegate, wait via IEventTracker, assert side effects
public static class EventTestingPatternSample
{
    public static async Task TestWelcomeEmailEvent(IServiceProvider sp, IEventTracker tracker)
    {
        var sendEmail = sp.GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();
        InMemoryEmailService.Clear();
        _ = sendEmail(Guid.NewGuid(), "test@example.com");
        await tracker.WaitAllAsync();
        Assert.Single(InMemoryEmailService.GetSentEmails());
    }
}
#endregion

#region events-testing-latch
// Multiple concurrent events - WaitAllAsync waits for all
public static class MultipleEventTestSample
{
    public static async Task TestMultipleConcurrentEvents(IServiceProvider sp, IEventTracker tracker)
    {
        var sendEmail = sp.GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();
        InMemoryEmailService.Clear();
        _ = sendEmail(Guid.NewGuid(), "emp1@example.com");
        _ = sendEmail(Guid.NewGuid(), "emp2@example.com");
        await tracker.WaitAllAsync();
        Assert.Equal(2, InMemoryEmailService.GetSentEmails().Count);
    }
}
#endregion
