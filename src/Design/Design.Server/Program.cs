// =============================================================================
// DESIGN SOURCE OF TRUTH: Server Configuration
// =============================================================================
//
// Demonstrates RemoteFactory server-side setup with ASP.NET Core.
//
// DESIGN DECISION: Minimal server setup
//
// The server only needs:
// 1. AddNeatooAspNetCore() - registers factory services and endpoints
// 2. UseNeatoo() - adds the middleware for handling factory requests
// 3. Service registrations for server-only dependencies
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.FactoryPatterns;
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Enable CORS for Blazor WASM client
builder.Services.AddCors();

// -------------------------------------------------------------------------
// DESIGN DECISION: AddNeatooAspNetCore registers everything needed
//
// This single call:
// - Scans the assembly for [Factory] types
// - Registers generated factory delegates
// - Configures the RemoteFactory middleware
// - Sets NeatooFactory.Mode = Server (remote operations execute here)
//
// COMMON MISTAKE: Forgetting to pass the assembly
//
// WRONG:
// builder.Services.AddNeatooAspNetCore();  // <-- No assembly = nothing registered
//
// RIGHT:
// builder.Services.AddNeatooAspNetCore(typeof(Order).Assembly);
// -------------------------------------------------------------------------
builder.Services.AddNeatooAspNetCore(typeof(Order).Assembly);

// -------------------------------------------------------------------------
// Register server-only services
//
// DESIGN DECISION: Server-only dependencies are registered here
//
// These services are only available on the server. They're injected into
// factory methods via [Service] parameters (method injection).
//
// The client container will NOT have these registrations, which is why
// calling a method with [Service] parameters from the client fails at
// runtime if it's not marked [Remote].
// -------------------------------------------------------------------------
builder.Services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IExampleRepository, ExampleRepository>();
builder.Services.AddScoped<IExampleService, ExampleService>();

var app = builder.Build();

// -------------------------------------------------------------------------
// DESIGN DECISION: UseNeatoo adds the RemoteFactory middleware
//
// This middleware:
// - Intercepts requests to the configured endpoint (default: /remotefactory)
// - Deserializes the factory operation request
// - Resolves the delegate and invokes the operation
// - Serializes the result back to the client
//
// No controllers needed - it's all handled by the middleware.
// -------------------------------------------------------------------------
app.UseNeatoo();

// Enable CORS for any origin (for demo purposes)
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// -------------------------------------------------------------------------
// Minimal API endpoint for health check
// -------------------------------------------------------------------------
app.MapGet("/", () => "Design.Server is running. RemoteFactory endpoint: /remotefactory");

await app.RunAsync();
