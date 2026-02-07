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
    }
}
#endregion

#region aspnetcore-custom-serialization
public static class CustomSerializationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Named format: human-readable JSON (useful for debugging)
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Named },
            typeof(Employee).Assembly);
    }
}
#endregion

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
