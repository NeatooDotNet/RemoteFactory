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
// 1. Service registration -- AddNeatooAspNetCore() for the generated factories and
//    the request handler, plus the server-only dependencies factory methods resolve by
//    [Service] injection. Both live in ServerServices.cs.
// 2. UseNeatoo() - maps the /api/neatoo endpoint and its correlation middleware
// 3. Hosted WASM middleware to serve the Blazor client
// 4. UseAuthentication and UseAuthorization before UseNeatoo, so HttpContext.User
//    is set when [AspAuthorize] and ICurrentUser read it
//
// The code below is embedded in getting started (region
// getting-started-server-program); the reasons are kept here so it stays short.
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
//
// Hosted Blazor WASM middleware: UseBlazorFrameworkFiles() serves the client's
// _framework files (blazor.webassembly.js, dotnet.wasm, etc.) and UseStaticFiles()
// its wwwroot content. They come BEFORE UseNeatoo() so static files are served
// directly.
//
// DESIGN DECISION: UseNeatoo adds the RemoteFactory middleware
//
// This middleware:
// - Maps the one endpoint, POST /api/neatoo (a fixed path; the client posts to it)
// - Reads the X-Correlation-Id header into ICorrelationContext and echoes it back
// - Deserializes the factory operation request
// - Resolves the delegate and invokes the operation
// - Serializes the result back to the client
//
// No controllers needed - it's all handled by the middleware.
//
// The SPA fallback comes AFTER all other middleware and route mappings so that
// API routes (/api/neatoo) are handled first.
//
// =============================================================================

using Design.Server;
using Neatoo.RemoteFactory.AspNetCore;   // for app.UseNeatoo() below

#region getting-started-server-program
var builder = WebApplication.CreateBuilder(args);

// RemoteFactory plus the server-only services [Service] parameters resolve (ServerServices.cs)
builder.Services.AddDesignServer();

var app = builder.Build();

// Serve the hosted Blazor WASM client
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Sign the caller in before the endpoint runs: the authorization checks read HttpContext.User
app.UseAuthentication();
app.UseAuthorization();

// The RemoteFactory endpoint: one route for every factory call, no controllers
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
#endregion

// Public so the HTTP tests can host this exact Program (WebApplicationFactory<Program>).
// .NET 10 makes a top-level Program public by itself; .NET 9 does not.
public partial class Program;
