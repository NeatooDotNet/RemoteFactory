namespace EmployeeManagement.Domain.Samples.Events;

#region events-tracker-generated
// Generated for [Event] method SendWelcomeEmail(Guid employeeId, string email, ...):
//   public delegate Task SendWelcomeEmailEvent(Guid employeeId, string email);
//
// Factory runs in Task.Run with new DI scope, tracks via IEventTracker:
//   var task = Task.Run(async () => {
//       using var scope = _sp.CreateScope();
//       var ct = _lifetime.ApplicationStopping;
//       await entity.SendWelcomeEmail(employeeId, email, emailService, ct);
//   });
//   _eventTracker.Track(task);
#endregion

public static class GeneratedEventCodeIllustration { }
