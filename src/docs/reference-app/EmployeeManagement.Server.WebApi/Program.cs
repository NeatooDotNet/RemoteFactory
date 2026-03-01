using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Infrastructure;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

// Hosted Blazor WASM middleware
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Add the /api/neatoo endpoint for remote calls
app.UseNeatoo();

// Fallback: serve index.html for unmatched routes (SPA routing)
app.MapFallbackToFile("index.html");
#endregion

await app.RunAsync();
