using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Infrastructure;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for Blazor WebAssembly client
builder.Services.AddCors();

#region server-configuration
// Configure RemoteFactory for Server mode with ASP.NET Core integration
var domainAssembly = typeof(Employee).Assembly;

builder.Services.AddNeatooAspNetCore(
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    domainAssembly);

// Register factory types from domain assembly (interfaces to implementations)
builder.Services.RegisterMatchingName(domainAssembly);
#endregion

// Register infrastructure services (repositories, etc.)
builder.Services.AddInfrastructureServices();

var app = builder.Build();

#region server-middleware
// Configure the Neatoo RemoteFactory endpoint
app.UseNeatoo();
#endregion

// Allow cross-origin requests from Blazor client
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.Run();
