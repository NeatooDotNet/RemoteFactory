using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.Samples.GettingStarted;
using EmployeeManagement.Infrastructure.Repositories;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples;

// Removed: duplicate snippet moved to Program.cs
public static class ServerProgram
{
    public static void ConfigureServer(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };
        builder.Services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(EmployeeModel).Assembly);
        builder.Services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        var app = builder.Build();
        app.UseNeatoo();
        app.Run();
    }
}
