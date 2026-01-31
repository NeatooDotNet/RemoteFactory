using System.Runtime.InteropServices;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-minimal-api
public static class MinimalApiSample
{
    public static void Configure(WebApplication app)
    {
        // RemoteFactory endpoint: POST /api/neatoo
        app.UseNeatoo();

        // Health check endpoint
        app.MapGet("/health", () => "OK");

        // Custom API endpoint alongside Neatoo
        app.MapGet("/api/info", () => new
        {
            Version = "1.0.0",
            Framework = RuntimeInformation.FrameworkDescription
        });

        // MVC controllers (optional)
        // app.MapControllers();
    }
}
#endregion
