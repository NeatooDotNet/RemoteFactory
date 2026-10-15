using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

// The other aspnetcore-* regions moved to src/Design in EXSET-006; this one
// moves with authentication and authorization in EXSET-009.

#region aspnetcore-middleware-order
public static class MiddlewareOrderSample
{
    public static void Configure(WebApplication app)
    {
        app.UseCors();           // 1. CORS first
        app.UseAuthentication(); // 2. Auth middleware
        app.UseAuthorization();
        app.UseNeatoo();         // 3. RemoteFactory endpoint (after auth)
    }
}
#endregion
