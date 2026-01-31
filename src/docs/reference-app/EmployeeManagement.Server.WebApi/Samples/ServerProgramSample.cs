using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.Samples.GettingStarted;
using EmployeeManagement.Infrastructure.Repositories;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples;

#region getting-started-server-program
public static class ServerProgram
{
    public static void ConfigureServer(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure serialization format (Ordinal is default, produces smaller payloads)
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };

        // Register RemoteFactory server-side services with domain assembly
        builder.Services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(EmployeeModel).Assembly);

        // Register server-only services (repositories, etc.)
        builder.Services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();

        var app = builder.Build();

        // Map the /api/neatoo endpoint for remote delegate requests
        app.UseNeatoo();

        app.Run();
    }
}
#endregion
