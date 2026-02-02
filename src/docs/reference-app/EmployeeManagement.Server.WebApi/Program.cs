using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Infrastructure;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for Blazor WebAssembly client
builder.Services.AddCors();

#region getting-started-server-program
// Register RemoteFactory services and domain assembly
builder.Services.AddNeatooAspNetCore(
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Register factory types (IEmployeeFactory -> EmployeeFactory)
builder.Services.RegisterMatchingName(typeof(Employee).Assembly);

// Register your infrastructure services
builder.Services.AddInfrastructureServices();

var app = builder.Build();

// Add the /api/neatoo endpoint for remote calls
app.UseNeatoo();
#endregion

// Allow cross-origin requests from Blazor client
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.Run();
