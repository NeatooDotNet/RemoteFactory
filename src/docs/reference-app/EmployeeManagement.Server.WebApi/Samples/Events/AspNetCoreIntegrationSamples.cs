namespace EmployeeManagement.Server.Samples.Events;

#region events-aspnetcore
// ASP.NET Core Integration for Events
//
// When you call services.AddNeatooAspNetCore(assembly), it registers:
//
// 1. IEventTracker (singleton)
//    - Tracks all pending fire-and-forget event Tasks
//    - Provides PendingCount property and WaitAllAsync() method
//    - Used by EventTrackerHostedService for graceful shutdown
//
// 2. EventTrackerHostedService (IHostedService)
//    - Implements graceful shutdown for events
//    - StopAsync waits for pending events to complete
//
// Shutdown sequence:
// 1. ApplicationStopping token is triggered (SIGTERM, app.StopAsync, etc.)
// 2. EventTrackerHostedService.StopAsync is called by the host
// 3. EventTrackerHostedService calls eventTracker.WaitAllAsync(ct)
// 4. Running events receive the cancellation signal
// 5. Events that check ct.IsCancellationRequested can exit early
// 6. Application waits for events to complete or shutdown timeout
// 7. Application exits cleanly
//
// This ensures events complete before the application stops,
// preventing data loss or incomplete operations.
#endregion

/// <summary>
/// Placeholder class to hold the comment block.
/// </summary>
public static class AspNetCoreEventIntegration
{
}
