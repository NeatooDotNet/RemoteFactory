using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-minimal-api
public static class MinimalApiSample
{
    public static void Configure(WebApplication app)
    {
        app.UseNeatoo();                           // POST /api/neatoo
        app.MapGet("/health", () => "OK");         // Custom endpoints coexist
    }
}
#endregion
