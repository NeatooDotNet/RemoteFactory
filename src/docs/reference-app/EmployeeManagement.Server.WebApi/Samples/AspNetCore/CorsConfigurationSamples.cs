using EmployeeManagement.Domain.Aggregates;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

// CORS is only needed when client and server are on different origins
// (non-hosted deployments). In hosted Blazor WASM mode, client and server
// share the same origin, so CORS configuration is unnecessary.
#region aspnetcore-cors
public static class CorsConfigurationSample
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Only needed for non-hosted deployments where client and server
        // are on different origins (e.g., separate Blazor WASM standalone app)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://client.example.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        app.UseCors();    // CORS must be before UseNeatoo
        app.UseNeatoo();

        app.Run();
    }
}
#endregion
