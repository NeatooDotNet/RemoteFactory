// =============================================================================
// DESIGN SOURCE OF TRUTH: Blazor Client Configuration (Hosted WASM)
// =============================================================================
//
// Demonstrates RemoteFactory client-side setup with Blazor WebAssembly.
// In hosted mode, the server serves the client, so they share the same origin.
//
// DESIGN DECISION: Client setup is minimal
//
// The client only needs:
// 1. AddNeatooRemoteFactory() - registers factory proxies
// 2. HttpClient using HostEnvironment.BaseAddress - for making remote calls
//
// =============================================================================

using Design.Client.Blazor;
using Design.Domain.Aggregates;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// -------------------------------------------------------------------------
// DESIGN DECISION: AddNeatooRemoteFactory registers client-side proxies
//
// Parameters:
// - NeatooFactory.Remote: Tells the client to serialize calls to server
// - typeof(IOrder).Assembly: Scans for [Factory] types to create proxies
//
// What this does:
// - Registers generated delegate types that serialize to server
// - Sets NeatooFactory.Mode = Remote
// - Does NOT register server-only services (they're not needed on client)
//
// COMMON MISTAKE: Using NeatooFactory.Logical on client
//
// WRONG:
// builder.Services.AddNeatooRemoteFactory(NeatooFactory.Logical, ...);
// // This makes all calls local - no server round-trip!
//
// RIGHT:
// builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, ...);
// -------------------------------------------------------------------------
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IOrder).Assembly);

// -------------------------------------------------------------------------
// DESIGN DECISION: Keyed HttpClient for RemoteFactory
//
// RemoteFactory uses a keyed service to find the HttpClient that points
// to the server. The key is RemoteFactoryServices.HttpClientKey.
//
// This allows the app to have multiple HttpClients for different purposes
// while RemoteFactory always uses the correct one.
//
// In hosted WASM mode, use builder.HostEnvironment.BaseAddress so the
// client automatically targets the server that hosts it -- no hardcoded
// URL needed.
// -------------------------------------------------------------------------
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Standard Blazor HttpClient for other purposes
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
