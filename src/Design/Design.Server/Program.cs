// =============================================================================
// DESIGN SOURCE OF TRUTH: Server Configuration (Hosted Blazor WASM)
// =============================================================================
//
// Demonstrates RemoteFactory server-side setup with ASP.NET Core.
// The server hosts the Blazor WASM client -- a single `dotnet run` starts
// both the API and the client application.
//
// DESIGN DECISION: Minimal server setup
//
// The server only needs:
// 1. AddNeatooAspNetCore() - registers factory services and endpoints
// 2. UseNeatoo() - adds the middleware for handling factory requests
// 3. Service registrations for server-only dependencies
// 4. Hosted WASM middleware to serve the Blazor client
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.FactoryPatterns;
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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
// builder.Services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
// -------------------------------------------------------------------------
builder.Services.AddNeatooAspNetCore(typeof(IOrder).Assembly);

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
// Hosted Blazor WASM middleware
//
// UseBlazorFrameworkFiles() configures the server to serve the client's
// _framework files (blazor.webassembly.js, dotnet.wasm, etc.).
// UseStaticFiles() serves the client's wwwroot content.
// These must come BEFORE UseNeatoo() so static files are served directly.
// -------------------------------------------------------------------------
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// -------------------------------------------------------------------------
// DESIGN DECISION: UseNeatoo adds the RemoteFactory middleware
//
// This middleware:
// - Intercepts requests to the configured endpoint (default: /api/neatoo)
// - Deserializes the factory operation request
// - Resolves the delegate and invokes the operation
// - Serializes the result back to the client
//
// No controllers needed - it's all handled by the middleware.
// -------------------------------------------------------------------------
app.UseNeatoo();

// -------------------------------------------------------------------------
// Fallback: serve index.html for unmatched routes (SPA routing)
//
// This must come AFTER all other middleware and route mappings so that
// API routes (/api/neatoo) are handled first.
// -------------------------------------------------------------------------
app.MapFallbackToFile("index.html");

await app.RunAsync();
