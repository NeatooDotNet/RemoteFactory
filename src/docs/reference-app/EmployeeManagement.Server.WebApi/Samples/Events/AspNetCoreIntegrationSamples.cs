namespace EmployeeManagement.Server.Samples.Events;

#region events-aspnetcore
// AddNeatooAspNetCore registers:
// - IEventTracker (singleton): tracks pending event Tasks, provides PendingCount/WaitAllAsync
// - EventTrackerHostedService: calls WaitAllAsync on shutdown for graceful completion
#endregion

public static class AspNetCoreEventIntegration { }
