using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-basic-setup
public static class BasicSetup
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register RemoteFactory services with the domain assembly
        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        // Map the /api/neatoo endpoint for remote delegate requests
        app.UseNeatoo();

        app.Run();
    }
}
#endregion

#region aspnetcore-addneatoo
public static class AddNeatooSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register with single domain assembly
        services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        // Register with multiple assemblies (if your domain spans multiple projects):
        // services.AddNeatooAspNetCore(
        //     typeof(Employee).Assembly,
        //     typeof(Department).Assembly);
    }
}
#endregion

#region aspnetcore-custom-serialization
public static class CustomSerializationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Named format produces larger but more readable JSON (useful for debugging)
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }
}
#endregion

#region aspnetcore-middleware-order
public static class MiddlewareOrderSample
{
    public static void Configure(WebApplication app)
    {
        // 1. CORS - must be first for cross-origin requests
        app.UseCors();

        // 2. Authentication/Authorization - before protected endpoints
        app.UseAuthentication();
        app.UseAuthorization();

        // 3. UseNeatoo - the /api/neatoo endpoint
        app.UseNeatoo();

        // 4. Other endpoints (controllers, minimal APIs, etc.)
        // app.MapControllers();
    }
}
#endregion
