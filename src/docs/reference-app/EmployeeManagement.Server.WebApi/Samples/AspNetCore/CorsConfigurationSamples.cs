using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-cors
public static class CorsConfigurationSample
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure default CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5001",  // Development
                        "https://myapp.example.com" // Production
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Required for auth cookies
            });

            // Named policy with specific headers for Neatoo API
            options.AddPolicy("NeatooApi", policy =>
            {
                policy.WithOrigins("http://localhost:5001")
                    .WithHeaders("Content-Type", "X-Correlation-Id")
                    .WithMethods("POST");
            });
        });

        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        // CORS must come before UseNeatoo
        app.UseCors();
        app.UseNeatoo();

        app.Run();
    }
}
#endregion
