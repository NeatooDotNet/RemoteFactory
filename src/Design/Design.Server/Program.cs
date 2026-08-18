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

using Design.Server;
using Neatoo.RemoteFactory.AspNetCore;   // for app.UseNeatoo() below

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------------
// Register everything: RemoteFactory itself plus the server-only services
// Design.Domain's factory methods resolve by [Service] injection.
//
// DESIGN DECISION: One named seam instead of a list of calls here
//
// The whole composition lives in ServerServices.cs so a test can call the
// same method and verify this server can actually serve the domain it hosts
// -- see DesignServerCompositionTests. When the registrations were inline
// here, four of them were simply missing and nothing caught it: the test
// harness has its own container and stayed green either way.
//
// The AddNeatooAspNetCore call is inside the seam too, not left here. With
// it out here, the test would have had to restate it, and a drifting
// assembly argument would have gone unnoticed for the same reason.
//
// Add new server-only services THERE, not here.
// -------------------------------------------------------------------------
builder.Services.AddDesignServer();

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
